static class RadarCurveExtensions
{
    public static RadarCurve WithValues(this RadarCurve curve, List<double> values)
    {
        foreach (var v in values)
        {
            curve.Values.Add(v);
        }
        return curve;
    }
}