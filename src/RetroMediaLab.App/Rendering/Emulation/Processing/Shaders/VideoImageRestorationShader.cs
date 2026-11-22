namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class VideoImageRestorationShader
{
    // Both backends supply a point sample of the unprocessed source in linear RGB.
    internal const string Source = """

        const float BandEdgeLimit = .075;
        const float BandFlatThreshold = .0015;

        float restorationMaxChannel(vec3 value)
        {
            value = abs(value);
            return max(value.r, max(value.g, value.b));
        }

        bool restorationBetween(vec3 sampleColor, vec3 endpoint)
        {
            float fraction = dot(sampleColor, endpoint) / dot(endpoint, endpoint);
            return fraction >= -.08 && fraction <= 1.08
                && restorationMaxChannel(sampleColor - endpoint * fraction) <= BandFlatThreshold * 2.0;
        }

        float restorationFirstStep(vec2 uv, vec2 direction, float radius, vec3 center, vec3 delta)
        {
            float nearOffset = 0.0;
            float farOffset = radius;
            float threshold = dot(delta, delta) * .5;
            for (int i = 0; i < 5; ++i)
            {
                if (farOffset - nearOffset <= 1.0) break;
                float middle = floor((nearOffset + farOffset) * .5);
                if (dot(restorationPoint(uv + direction * middle) - center, delta) >= threshold)
                    farOffset = middle;
                else
                    nearOffset = middle;
            }
            return farOffset;
        }

        bool restorationBand(vec2 uv, vec2 axis, float available, vec3 center, out vec3 correction)
        {
            correction = vec3(0.0);
            float radius = 2.0;
            for (int i = 0; i < 5; ++i, radius *= 2.0)
            {
                if (radius > available) break;
                vec2 offset = axis * radius;
                vec3 before = restorationPoint(uv - offset) - center;
                vec3 after = restorationPoint(uv + offset) - center;
                float beforeMagnitude = restorationMaxChannel(before);
                float afterMagnitude = restorationMaxChannel(after);
                float span = max(beforeMagnitude, afterMagnitude);
                if (span > BandEdgeLimit) return false;
                if (min(beforeMagnitude, afterMagnitude) < max(BandFlatThreshold, span * .25)) continue;
                if (dot(before, after) >= -.85 * length(before) * length(after)) return false;
                if (!restorationBetween(restorationPoint(uv - offset * .5) - center, before)
                    || !restorationBetween(restorationPoint(uv + offset * .5) - center, after)) return false;

                float leftBoundary = restorationFirstStep(uv, -axis, radius, center, before);
                float rightBoundary = restorationFirstStep(uv, axis, radius, center, after);
                float position = (leftBoundary - .5) / (leftBoundary + rightBoundary - 1.0);
                correction = mix(before, after, position) * .5;
                return true;
            }
            return false;
        }

        vec3 restorationLostDetail(vec3 center, vec3 filtered, vec3 before, vec3 after)
        {
            vec3 detail = center - (before + after) * .5;
            float amplitude = restorationMaxChannel(detail);
            if (amplitude < .018 || restorationMaxChannel(before - after) > amplitude * .45)
                return vec3(0.0);
            float missing = clamp(dot(center - filtered, detail) / dot(detail, detail), 0.0, 1.0);
            return clamp(filtered + detail * missing, min(center, filtered), max(center, filtered)) - filtered;
        }

        vec3 restorationDetailAlong(vec2 uv, vec2 axis, float available,
            vec3 center, vec3 filtered, vec3 before, vec3 after)
        {
            if (available >= 2.0)
            {
                if (restorationMaxChannel(center - before) <= BandFlatThreshold
                    && restorationMaxChannel(center - after) >= .018)
                    before = restorationPoint(uv - axis * 2.0);
                if (restorationMaxChannel(center - after) <= BandFlatThreshold
                    && restorationMaxChannel(center - before) >= .018)
                    after = restorationPoint(uv + axis * 2.0);
            }
            return restorationLostDetail(center, filtered, before, after);
        }

        vec3 restoreHostSource(vec3 filtered, vec2 uv, vec2 sourceSize, float deband, float details)
        {
            if (deband <= 0.0 && details <= 0.0) return filtered;
            vec2 coordinate = floor(uv * sourceSize);
            vec2 available = min(coordinate, sourceSize - coordinate - 1.0);
            if (min(available.x, available.y) < 1.0) return filtered;
            vec2 centerUv = (coordinate + .5) / sourceSize;
            vec2 dx = vec2(1.0 / sourceSize.x, 0.0);
            vec2 dy = vec2(0.0, 1.0 / sourceSize.y);
            vec3 center = restorationPoint(centerUv);
            vec3 left = restorationPoint(centerUv - dx);
            vec3 right = restorationPoint(centerUv + dx);
            vec3 up = restorationPoint(centerUv - dy);
            vec3 down = restorationPoint(centerUv + dy);
            vec3 result = filtered;
            if (deband > 0.0
                && restorationMaxChannel(left - center) <= BandEdgeLimit
                && restorationMaxChannel(right - center) <= BandEdgeLimit
                && restorationMaxChannel(up - center) <= BandEdgeLimit
                && restorationMaxChannel(down - center) <= BandEdgeLimit)
            {
                vec3 correction;
                bool band = restorationBand(centerUv, dx, available.x, center, correction);
                if (!band) band = restorationBand(centerUv, dy, available.y, center, correction);
                if (band)
                {
                    // Spatial noise is independent of time and restricted to detected bands.
                    vec2 pixel = floor(gl_FragCoord.xy);
                    float noise = fract(52.9829189 * fract(dot(pixel, vec2(.06711056, .00583715)))) - .5;
                    float luma = clamp(dot(center, vec3(.2126, .7152, .0722)), 0.0, 1.0);
                    float quantizationStep = (.08 + 2.0 * sqrt(luma)) / 255.0;
                    result += (correction + vec3(noise * quantizationStep)) * deband;
                }
            }
            if (details > 0.0)
            {
                vec3 horizontal = restorationDetailAlong(centerUv, dx, available.x, center, filtered, left, right);
                vec3 vertical = restorationDetailAlong(centerUv, dy, available.y, center, filtered, up, down);
                vec3 lost = dot(horizontal, horizontal) >= dot(vertical, vertical) ? horizontal : vertical;
                vec2 fromCenter = abs(fract(uv * sourceSize) - .5);
                float weight = clamp((.5 - max(fromCenter.x, fromCenter.y)) * 4.0, 0.0, 1.0) * details;
                result += lost * weight;
            }
            return clamp(result, 0.0, 1.0);
        }

        """;
}
