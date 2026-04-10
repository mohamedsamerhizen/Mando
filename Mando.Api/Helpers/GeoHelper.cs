namespace Mando.Api.Helpers;

public static class GeoHelper
{
    public static double CalculateDistanceInMeters(
        decimal latitude1,
        decimal longitude1,
        decimal latitude2,
        decimal longitude2)
    {
        const double earthRadiusMeters = 6371000;

        var lat1Rad = DegreesToRadians((double)latitude1);
        var lon1Rad = DegreesToRadians((double)longitude1);
        var lat2Rad = DegreesToRadians((double)latitude2);
        var lon2Rad = DegreesToRadians((double)longitude2);

        var deltaLat = lat2Rad - lat1Rad;
        var deltaLon = lon2Rad - lon1Rad;

        var a =
            Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
            Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
            Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180d);
    }
}