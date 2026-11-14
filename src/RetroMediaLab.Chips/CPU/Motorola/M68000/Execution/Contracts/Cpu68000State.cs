namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

public struct Cpu68000State
{
    public int cycles, refresh_cycles;
    public uint cycle_end, pc, prev_pc, ir;
    public uint t1_flag, s_flag, x_flag, n_flag, not_z_flag, v_flag, c_flag;
    public uint int_mask, int_level, stopped, pref_addr, pref_data;
    public uint instr_mode, run_mode, aerr_enabled, aerr_address, aerr_write_mode, aerr_fc;
    public uint tracing, address_space;
    public uint poll_pc, poll_cycle, poll_detected;
}

