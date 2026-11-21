namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
internal abstract partial class SegaBoard
{
    private sealed class MegaDriveFrameScheduler(SegaBoard board)
    {
        private readonly SegaBoard _board = board;
        private readonly Action<uint>? _audioSynchronizer = board.MachineAudioSynchronizer;
        public void Execute(int do_skip)
        {
            unchecked
            {
                int start = default;
                int end = default;
                int line = default;
                _board.mcycles_vdp = unchecked((uint)(0));
                _board.fifo_cycles[(int)(0)] = unchecked((uint)(0));
                _board.fifo_cycles[(int)(1)] = unchecked((uint)(0));
                _board.fifo_cycles[(int)(2)] = unchecked((uint)(0));
                _board.fifo_cycles[(int)(3)] = unchecked((uint)(0));
                if (((_board.viewport_changed & 2) != 0))
                {
                    int old_interlaced = unchecked((int)(_board.interlaced));
                    _board.interlaced = unchecked((byte)(((unchecked((int)(_board.reg[(int)(12)])) & 2) >> 1)));
                    if ((old_interlaced != unchecked((int)(_board.interlaced))))
                    {
                        _board.im2_flag = (((unchecked((int)(_board.reg[(int)(12)])) & 6) == 6) ? (byte)1 : (byte)0);
                        _board.odd_frame = _board.interlaced;
                        _board.viewport_changed = 5;
                        if (((unchecked((int)(_board.reg[(int)(1)])) & 4) != 0))
                        {
                            _board.SelectMode5Renderers();
                        }
                    }
                    else
                    {
                        _board.viewport_changed &= (~2);
                    }

                    if (((unchecked((int)(_board.reg[(int)(1)])) & 4) != 0))
                    {
                        if (((unchecked((int)(_board.reg[(int)(1)])) & 8) != 0))
                        {
                            _board.viewport_h = 240;
                            _board.viewport_y = (((_board.overscan & 1) * 24) * unchecked((int)(_board.vdp_pal)));
                        }
                        else
                        {
                            _board.viewport_h = 224;
                            _board.viewport_y = ((_board.overscan & 1) * (8 + (24 * unchecked((int)(_board.vdp_pal)))));
                        }
                    }
                    else
                    {
                        _board.viewport_h = 192;
                        _board.viewport_y = (((_board.overscan & 1) * 24) * (unchecked((int)(_board.vdp_pal)) + 1));
                    }

                    _board.viewport_w = (256 + ((unchecked((int)(_board.reg[(int)(12)])) & 1) << 6));
                    if ((_board.viewport_h != _board.viewport_oh))
                    {
                        _board.viewport_oh = _board.viewport_h;
                        _board.viewport_changed |= 1;
                    }
                }

                if ((_board.viewport_y != 0))
                {
                    _board.blank_line(_board.viewport_h, (-_board.viewport_x), (_board.viewport_w + (2 * _board.viewport_x)));
                }

                _board.status &= unchecked((ushort)(65261));
                _board.status |= unchecked((ushort)(8));
                if ((_board.interlaced != 0))
                {
                    _board.odd_frame ^= unchecked((byte)(1));
                    _board.status |= unchecked((ushort)((unchecked((int)(_board.odd_frame)) << 4)));
                }

                if ((_board.dma_length != 0))
                {
                    _board.vdp_dma_update(unchecked((uint)(0)));
                }

                _board.input_refresh();
                if ((unchecked((int)(_board.h_counter)) == 0))
                {
                    _board.hint_pending = unchecked((byte)(16));
                    if (((unchecked((int)(_board.reg[(int)(0)])) & 16) != 0))
                    {
                        _board.m68k_update_irq(unchecked((uint)(4)));
                    }
                }

                _board.osd_input_update();
                if ((unchecked((int)(_board.v_counter)) != _board.viewport_h))
                {
                    _board.v_counter = unchecked((ushort)(_board.viewport_h));
                    _board.m68k_run(_board.vint_cycle);
                    _audioSynchronizer?.Invoke(unchecked((uint)_board.main.cycles));
                    if ((unchecked((int)(_board.zstate)) == 1))
                    {
                        _board.z80_run(_board.vint_cycle);
                    }

                    _board.status |= unchecked((ushort)(128));
                    _board.vint_pending = unchecked((byte)(32));
                    if (((unchecked((int)(_board.reg[(int)(1)])) & 32) != 0))
                    {
                        _board.m68k_set_irq(unchecked((uint)(6)));
                    }

                    _board.z80.irq_state = 1;
                }

                _board.m68k_run(unchecked((uint)(3420)));
                _audioSynchronizer?.Invoke(unchecked((uint)_board.main.cycles));
                if ((unchecked((int)(_board.zstate)) == 1))
                {
                    _board.z80_run(unchecked((uint)(3420)));
                }

                _board.z80.irq_state = 0;
                if ((_board.HasSvp != 0))
                {
                    _board.ssp1601_run(unchecked((int)(_board.SVP_cycles)));
                }

                _board.mcycles_vdp = unchecked((uint)(3420));
                line = (_board.viewport_h + 1);
                start = (unchecked((int)(_board.lines_per_frame)) - _board.viewport_y);
                end = (_board.viewport_h + _board.viewport_y);
                do
                {
                    _board.v_counter = unchecked((ushort)(line));
                    if (((line < end) || (line >= start)))
                    {
                        _board.blank_line(line, (-_board.viewport_x), (_board.viewport_w + (2 * _board.viewport_x)));
                    }

                    _board.input_refresh();
                    _board.m68k_run((_board.mcycles_vdp + unchecked((uint)(3420))));
                    _audioSynchronizer?.Invoke(unchecked((uint)_board.main.cycles));
                    if ((unchecked((int)(_board.zstate)) == 1))
                    {
                        _board.z80_run((_board.mcycles_vdp + unchecked((uint)(3420))));
                    }

                    if ((_board.HasSvp != 0))
                    {
                        _board.ssp1601_run(unchecked((int)(_board.SVP_cycles)));
                    }

                    _board.mcycles_vdp += unchecked((uint)(3420));
                }
                while (((++line) < (unchecked((int)(_board.lines_per_frame)) - 1)));
                _board.v_counter = unchecked((ushort)(line));
                if ((_board.viewport_y != 0))
                {
                    _board.blank_line(line, (-_board.viewport_x), (_board.viewport_w + (2 * _board.viewport_x)));
                }

                _board.h_counter = _board.reg[(int)(10)];
                _board.status &= unchecked((ushort)((~8)));
                if ((_board.dma_length != 0))
                {
                    _board.vdp_dma_update(_board.mcycles_vdp);
                }

                if (((unchecked((int)(_board.reg[(int)(1)])) & 64) != 0))
                {
                    _board.parse_satb((-1));
                }

                _board.input_refresh();
                _board.m68k_run((_board.mcycles_vdp + unchecked((uint)(3420))));
                _audioSynchronizer?.Invoke(unchecked((uint)_board.main.cycles));
                if ((unchecked((int)(_board.zstate)) == 1))
                {
                    _board.z80_run((_board.mcycles_vdp + unchecked((uint)(3420))));
                }

                if ((_board.HasSvp != 0))
                {
                    _board.ssp1601_run(unchecked((int)(_board.SVP_cycles)));
                }

                _board.mcycles_vdp += unchecked((uint)(3420));
                line = 0;
                do
                {
                    _board.v_counter = unchecked((ushort)(line));
                    if ((_board.dma_length != 0))
                    {
                        _board.vdp_dma_update(_board.mcycles_vdp);
                    }

                    if ((!(do_skip != 0)))
                    {
                        _board.render_line(line);
                    }

                    _board.input_refresh();
                    if ((unchecked((int)(_board.h_counter)) == 0))
                    {
                        _board.h_counter = _board.reg[(int)(10)];
                        _board.hint_pending = unchecked((byte)(16));
                        if (((unchecked((int)(_board.reg[(int)(0)])) & 16) != 0))
                        {
                            _board.m68k_update_irq(unchecked((uint)(4)));
                        }
                    }
                    else
                    {
                        _board.h_counter--;
                    }

                    _board.m68k_run((_board.mcycles_vdp + unchecked((uint)(3420))));
                    _audioSynchronizer?.Invoke(unchecked((uint)_board.main.cycles));
                    if ((unchecked((int)(_board.zstate)) == 1))
                    {
                        _board.z80_run((_board.mcycles_vdp + unchecked((uint)(3420))));
                    }

                    if ((_board.HasSvp != 0))
                    {
                        _board.ssp1601_run(unchecked((int)(_board.SVP_cycles)));
                    }

                    _board.mcycles_vdp += unchecked((uint)(3420));
                }
                while (((++line) < _board.viewport_h));
                if ((_board.viewport_w != _board.viewport_ow))
                {
                    _board.viewport_ow = _board.viewport_w;
                    _board.viewport_changed |= 1;
                }

                _board.input_end_frame(_board.mcycles_vdp);
                _board.main.refresh_cycles -= unchecked((int)(_board.mcycles_vdp));
                _board.main.cycles -= unchecked((int)(_board.mcycles_vdp));
                _board.z80.cycles -= _board.mcycles_vdp;
                _board.dma_endCycles = unchecked((uint)(0));
            }
        }
    }
}
