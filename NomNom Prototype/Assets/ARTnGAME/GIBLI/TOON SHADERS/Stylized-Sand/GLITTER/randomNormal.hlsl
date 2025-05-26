// Generate a random float between 0 and 1 using the pixel position and seed
float RandomFloat(float2 seed)
{
    return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453) + 0.00001;
}


// Generate random numbers in a normal distribution using Box-Muller transform
void GenerateNormalRandom_float(float2 seed, float min, float max, out float3 Out)
{
    // Pseudo-random numbers between 0 and 1
    float u1 = RandomFloat(seed);
    float u2 = RandomFloat(seed + float2(132.54, 465.32));

    // Box-Muller transform to convert uniform distribution to normal distribution
    float radius = sqrt(-2.0 * log(u1)) * 4.0;
    float theta = 2.0 * 3.1415926535897932384626433832795 * u2;

    // Convert polar coordinates to Cartesian coordinates
    float x = radius * cos(theta);
    float y = radius * sin(theta);

    // Scale and shift the results to the desired range [min, max]
    float2 result;
    result.x = x * (max - min) + (min);
    result.y = y * (max - min) + (min);

    Out = float3(result.xy, 1);
}