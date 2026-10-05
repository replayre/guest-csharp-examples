public static class VehicleManager
{
    static Dictionary<TDBID, List<CName>> vehicles = new Dictionary<TDBID, List<CName>>
    {
        {
            new TDBID("Vehicle.v_sport1_rayfield_caliburn"),
            new List<CName>
            {
                new CName("rayfield_caliburn__basic_premium_00"),
                new CName("rayfield_caliburn__basic_premium_01"),
                new CName("rayfield_caliburn__basic_premium_02"),
                new CName("rayfield_caliburn__basic_premium_03"),
                new CName("rayfield_caliburn__basic_premium_04"),
            }
        },
        {
            new TDBID("Vehicle.v_standard2_makigai_maimai"),
            new List<CName>
            {
                new CName("makigai_maimai__basic_urban_01"),
                new CName("makigai_maimai__basic_urban_02"),
                new CName("makigai_maimai__basic_urban_03"),
                new CName("makigai_maimai__basic_urban_05"),
            }
        },
        {
            new TDBID("Vehicle.v_sport2_porsche_911turbo"),
            new List<CName> { new CName("porsche_911turbo__basic_johnny") }
        },
        {
            new TDBID("Vehicle.v_sport2_porsche_911turbo_cabrio"),
            new List<CName> { new CName("porsche_911turbo__basic_cabrio_01") }
        },
        {
            new TDBID("Vehicle.v_sport2_quadra_type66"),
            new List<CName>
            {
                new CName("quadra_type66__basic_sampson"),
                new CName("quadra_type66__basic_sampson"),
            }
        },
    };

    public static TDBID GetRandomVehicle()
    {
        var randomIndex = Random.Shared.Next(vehicles.Count);
        return new List<TDBID>(vehicles.Keys)[randomIndex];
    }

    public static CName GetRandomAppearanceForVehicle(TDBID vehicleId)
    {
        if (!vehicles.TryGetValue(vehicleId, out var appearances))
        {
            return new CName("default");
        }

        var randomIndex = Random.Shared.Next(appearances.Count);
        return appearances[randomIndex];
    }
}
