namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
internal abstract partial class SegaBoard
{
    private sealed class EightBitFrameScheduler(SegaBoard board)
    {
        private readonly SegaBoard _board = board;
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
                    _board.viewport_changed &= (~2);
                    if (((unchecked((int)(_board.system_hw)) & 128) != 0))
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
                    }
                    else
                    {
                        int mode = ((unchecked((int)(_board.reg[(int)(0)])) & 6) | (unchecked((int)(_board.reg[(int)(1)])) & 24));
                        if ((mode == 14))
                        {
                            _board.viewport_h = 240;
                        }
                        else if ((mode == 22))
                        {
                            _board.viewport_h = 224;
                        }
                        else
                        {
                            _board.viewport_h = 192;
                        }

                        if (((_board.overscan & 1) != 0))
                        {
                            _board.viewport_y = (((240 + (48 * unchecked((int)(_board.vdp_pal)))) - _board.viewport_h) >> 1);
                        }
                        else
                        {
                            if (((unchecked((int)(_board.system_hw)) == 64) && (!(_board.gg_extra != 0))))
                            {
                                _board.viewport_y = ((144 - _board.viewport_h) / 2);
                            }
                            else
                            {
                                _board.viewport_y = 0;
                            }
                        }
                    }

                    _board.viewport_w = (256 + ((unchecked((int)(_board.reg[(int)(12)])) & 1) << 6));
                    if ((_board.viewport_h != _board.viewport_oh))
                    {
                        _board.viewport_oh = _board.viewport_h;
                        _board.viewport_changed |= 1;
                    }
                }

                _board.v_counter = unchecked((ushort)(_board.viewport_h));
                if ((_board.viewport_y > 0))
                {
                    _board.blank_line(unchecked((int)(_board.v_counter)), (-_board.viewport_x), (_board.viewport_w + (2 * _board.viewport_x)));
                }

                if (((unchecked((int)(_board.system_hw)) & 128) != 0))
                {
                    _board.status &= unchecked((ushort)(237));
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
                }

                _board.input_refresh();
                if ((unchecked((int)(_board.h_counter)) == 0))
                {
                    _board.hint_pending = unchecked((byte)(16));
                    if (((unchecked((int)(_board.reg[(int)(0)])) & 16) != 0))
                    {
                        if (((_board.z80.cycles % unchecked((uint)(3420))) == unchecked((uint)(0))))
                        {
                            _board.z80_run((_board.z80.cycles + unchecked((uint)(1))));
                        }

                        _board.z80.irq_state = 1;
                    }
                }

                _board.osd_input_update();
                _board.z80_run(unchecked((uint)(3420)));
                if ((unchecked((int)(_board.v_counter)) == _board.viewport_h))
                {
                    _board.status |= unchecked((ushort)(128));
                    _board.vint_pending = unchecked((byte)(32));
                    if (((unchecked((int)(_board.reg[(int)(1)])) & 32) != 0))
                    {
                        _board.z80.irq_state = 1;
                    }
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
                        if (((unchecked((int)(_board.system_hw)) < 128) && (line > (unchecked((int)(_board.lines_per_frame)) - 16))))
                        {
                            if (((unchecked((int)(_board.reg[(int)(1)])) & 64) != 0))
                            {
                                _board.render_obj(((line - unchecked((int)(_board.lines_per_frame))) & 1));
                            }

                            _board.parse_satb((line - unchecked((int)(_board.lines_per_frame))));
                        }

                        _board.blank_line(line, (-_board.viewport_x), (_board.viewport_w + (2 * _board.viewport_x)));
                    }

                    _board.input_refresh();
                    _board.z80_run((_board.mcycles_vdp + unchecked((uint)(3420))));
                    _board.mcycles_vdp += unchecked((uint)(3420));
                }
                while (((++line) < (unchecked((int)(_board.lines_per_frame)) - 1)));
                _board.v_counter = unchecked((ushort)(line));
                if ((_board.viewport_y > 0))
                {
                    if ((unchecked((int)(_board.system_hw)) < 128))
                    {
                        if (((unchecked((int)(_board.reg[(int)(1)])) & 64) != 0))
                        {
                            _board.render_obj(1);
                        }
                    }

                    _board.blank_line(line, (-_board.viewport_x), (_board.viewport_w + (2 * _board.viewport_x)));
                }

                _board.h_counter = _board.reg[(int)(10)];
                if ((unchecked((int)(_board.system_hw)) != 64))
                {
                    if (((unchecked((int)(_board.input_pad[(int)(0)])) & 128) != 0))
                    {
                        if ((!(_board.pause_b != 0)))
                        {
                            _board.pause_b = unchecked((byte)(1));
                            _board.z80_set_nmi_line(unchecked((uint)(1)));
                            _board.z80_set_nmi_line(unchecked((uint)(0)));
                        }
                    }
                    else
                    {
                        _board.pause_b = unchecked((byte)(0));
                    }
                }

                do_skip |= ((unchecked((int)(_board.work_ram[(int)(8187)])) & unchecked((int)(_board.cart.special))) & 1);
                if (((unchecked((int)(_board.system_hw)) & 128) != 0))
                {
                    _board.status &= unchecked((ushort)((~8)));
                    if ((_board.dma_length != 0))
                    {
                        _board.vdp_dma_update(_board.mcycles_vdp);
                    }

                    if (((unchecked((int)(_board.reg[(int)(1)])) & 64) != 0))
                    {
                        _board.parse_satb((-1));
                    }
                }
                else
                {
                    _board.parse_satb((-1));
                }

                _board.input_refresh();
                _board.z80_run((_board.mcycles_vdp + unchecked((uint)(3420))));
                _board.mcycles_vdp += unchecked((uint)(3420));
                _board.vscroll = unchecked((ushort)(_board.reg[(int)(9)]));
                line = 0;
                do
                {
                    if ((_board.dma_length != 0))
                    {
                        _board.vdp_dma_update(_board.mcycles_vdp);
                    }

                    if ((unchecked((int)(_board.v_counter)) != line))
                    {
                        _board.v_counter = unchecked((ushort)(line));
                        if ((!(do_skip != 0)))
                        {
                            _board.render_line(line);
                        }
                    }

                    _board.input_refresh();
                    if ((unchecked((int)(_board.h_counter)) == 0))
                    {
                        _board.h_counter = _board.reg[(int)(10)];
                        _board.hint_pending = unchecked((byte)(16));
                        if (((unchecked((int)(_board.reg[(int)(0)])) & 16) != 0))
                        {
                            if (((_board.z80.cycles % unchecked((uint)(3420))) == unchecked((uint)(0))))
                            {
                                _board.z80_run((_board.z80.cycles + unchecked((uint)(1))));
                            }

                            _board.z80.irq_state = 1;
                        }
                    }
                    else
                    {
                        _board.h_counter--;
                    }

                    _board.z80_run((_board.mcycles_vdp + unchecked((uint)(3420))));
                    _board.mcycles_vdp += unchecked((uint)(3420));
                }
                while (((++line) < _board.viewport_h));
                if ((_board.viewport_w != _board.viewport_ow))
                {
                    _board.viewport_ow = _board.viewport_w;
                    _board.viewport_changed |= 1;
                }

                _board.input_end_frame(_board.mcycles_vdp);
                _board.z80.cycles -= _board.mcycles_vdp;
            }
        }
    }
}
