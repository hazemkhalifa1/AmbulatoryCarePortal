using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Constants;

public static class ClinicTypeStandards
{
    public static readonly string[] AMB = { "LD", "PC", "LB", "RD", "DN", "MM", "MOI", "IPC", "FMS", "DPU", "DA" };
    public static readonly string[] Dental = { "LD", "PC", "DL", "MOI", "IPC", "FMS" };

    public static string[] GetStandards(ClinicType type) =>
        type == ClinicType.Dental ? Dental : AMB;
}
