namespace PawsAndCare.Building
{
    // Broad class of a blueprint, used to filter the catalog and drive placement rules
    // (e.g. only stations register with the dispatch system). Serialized: append new values at the end only.
    public enum BlueprintCategory
    {
        STATION = 0,
        FURNITURE = 1,
        DECORATION = 2
    }
}
