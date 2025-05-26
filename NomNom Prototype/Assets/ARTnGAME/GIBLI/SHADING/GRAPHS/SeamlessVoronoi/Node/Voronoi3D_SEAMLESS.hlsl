inline float3 modulo(float3 divident, float3 divisor) {
	float3 positiveDivident = divident % divisor + divisor;
	return positiveDivident % divisor;
}

float rand3dTo1d(float3 value, float3 dotDir = float3(12.9898, 78.233, 37.719)) {
	//make value smaller to avoid artefacts
	float3 smallValue = sin(value);
	//get scalar value from 3d vector
	float random = dot(smallValue, dotDir);
	//make value more random by making it bigger and then taking the factional part
	random = frac(sin(random) * 143758.5453);
	return random;
}

float3 rand3dTo3d(float3 value) {
	return float3(
		rand3dTo1d(value, float3(12.989, 78.233, 37.719)),
		rand3dTo1d(value, float3(39.346, 11.135, 83.155)),
		rand3dTo1d(value, float3(73.156, 52.235, 09.151))
		);
}

void voronoiNoise_float(float2 UV, float Height, float CellDensity, float3 period, out float Voronoi, out float Cells, out float CellBorder, out float2 Center) {//float3 voronoiNoise(float3 value, float3 period) {
	
	//v0.1
	float3 value = float3(UV, Height) * CellDensity;	
	
	float3 baseCell = floor(value);

	//first pass to find the closest cell
	float minDistToCell = 10;
	float3 toClosestCell;
	float3 closestCell;
	[unroll]
	for (int x1 = -1; x1 <= 1; x1++) {
		[unroll]
		for (int y1 = -1; y1 <= 1; y1++) {
			[unroll]
			for (int z1 = -1; z1 <= 1; z1++) {
				float3 cell = baseCell + float3(x1, y1, z1);
				float3 tiledCell = modulo(cell, period);
				float3 cellPosition = cell + rand3dTo3d(tiledCell);
				float3 toCell = cellPosition - value;
				float distToCell = length(toCell);
				if (distToCell < minDistToCell) {
					minDistToCell = distToCell;
					closestCell = cell;
					toClosestCell = toCell;

					Center = (toCell + cell + baseCell) / CellDensity;
				}
			}
		}
	}
	///Center = float2(1, 1);
	//second pass to find the distance to the closest edge
	float minEdgeDistance = 10;
	[unroll]
	for (int x2 = -1; x2 <= 1; x2++) {
		[unroll]
		for (int y2 = -1; y2 <= 1; y2++) {
			[unroll]
			for (int z2 = -1; z2 <= 1; z2++) {
				float3 cell = baseCell + float3(x2, y2, z2);
				float3 tiledCell = modulo(cell, period);
				float3 cellPosition = cell + rand3dTo3d(tiledCell);
				float3 toCell = cellPosition - value;

				float3 diffToClosestCell = abs(closestCell - cell);
				bool isClosestCell = diffToClosestCell.x + diffToClosestCell.y + diffToClosestCell.z < 0.1;
				if (!isClosestCell) {
					float3 toCenter = (toClosestCell + toCell) * 0.5;
					float3 cellDifference = normalize(toCell - toClosestCell);
					float edgeDistance = dot(toCenter, cellDifference);
					minEdgeDistance = min(minEdgeDistance, edgeDistance);
				}
			}
		}
	}

	float random = rand3dTo1d(closestCell);
	//return float3(minDistToCell, random, minEdgeDistance);
	float3 outA = float3(minDistToCell, random, minEdgeDistance);

	
	float3 noise = outA;// voronoiNoise(value, Period);
	Cells = noise.y;
	Voronoi = noise.x;
	CellBorder = noise.z;
	Center = float2(1, 1);

}














inline float3 voronoi_noise_randomVector (float3 UV, float offset){
    float3x3 m = float3x3(15.27, 47.63, 99.41, 89.98, 95.07, 38.39, 33.83, 51.06, 60.77);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float3(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5, sin(UV.z*offset)*0.5+0.5);
}



//v0.1
void VoronoiPrecise3DWATA_float(float3 UV, float AngleOffset, float CellDensity, out float Out, out float Cells, out float2 Center) {
	float3 g = floor(UV * CellDensity);
	float3 f = frac(UV * CellDensity);
	float2 res = float2(8.0, 8.0);
	float3 ml = float3(0, 0, 0);
	float3 mv = float3(0, 0, 0);

	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			for (int z = -1; z <= 1; z++) {
				float3 lattice = float3(x, y, z);
				float3 offset = voronoi_noise_randomVector(g + lattice, AngleOffset);
				float3 v = lattice + offset - f;
				float d = dot(v, v);

				if (d < res.x) {
					res.x = d;
					res.y = offset.x;
					mv = v;
					ml = lattice;

					Center = (offset + lattice + g) / CellDensity;
				}
			}
		}
	}

	Cells = res.y;

	res = float2(8.0, 8.0);
	for (int y1 = -2; y1 <= 2; y1++) {
		for (int x1 = -2; x1 <= 2; x1++) {
			for (int z1 = -2; z1 <= 2; z1++) {
				float3 lattice = ml + float3(x1, y1, z1);
				float3 offset = voronoi_noise_randomVector(g + lattice, AngleOffset);
				float3 v = lattice + offset - f;

				float3 cellDifference = abs(ml - lattice);
				if (cellDifference.x + cellDifference.y + cellDifference.z > 0.1) {
					float d = dot(0.5*(mv + v), normalize(v - mv));
					res.x = min(res.x, d);
				}
			}
		}
	}

	Out = res.x;
}






void VoronoiPrecise3D_float(float3 UV, float AngleOffset, float CellDensity, out float Out, out float Cells) {
    float3 g = floor(UV * CellDensity);
    float3 f = frac(UV * CellDensity);
    float2 res = float2(8.0, 8.0);
    float3 ml = float3(0,0,0);
    float3 mv = float3(0,0,0);
 
    for(int y=-1; y<=1; y++){
        for(int x=-1; x<=1; x++){
            for(int z=-1; z<=1; z++){
                float3 lattice = float3(x, y, z);
                float3 offset = voronoi_noise_randomVector(g + lattice, AngleOffset);
                float3 v = lattice + offset - f;
                float d = dot(v, v);
    
                if(d < res.x){
                    res.x = d;
                    res.y = offset.x;
                    mv = v;
                    ml = lattice;
                }
            }
        }
    }
 
    Cells = res.y;
 
    res = float2(8.0, 8.0);
    for(int y1=-2; y1<=2; y1++){
        for(int x1=-2; x1<=2; x1++){
            for(int z1=-2; z1<=2; z1++){
                float3 lattice = ml + float3(x1, y1, z1);
                float3 offset = voronoi_noise_randomVector(g + lattice, AngleOffset);
                float3 v = lattice + offset - f;
    
                float3 cellDifference = abs(ml - lattice);
                if (cellDifference.x + cellDifference.y + cellDifference.z > 0.1){
                    float d = dot(0.5*(mv+v), normalize(v-mv));
                    res.x = min(res.x, d);
                }
            }
        }
    }
 
    Out = res.x;
}

void Voronoi3D_float(float3 UV, float AngleOffset, float CellDensity, out float Out, out float Cells, out float2 Center) {
    float3 g = floor(UV * CellDensity);
    float3 f = frac(UV * CellDensity);
    float3 res = float3(8.0, 8.0, 8.0);
 
    for(int y=-1; y<=1; y++){
        for(int x=-1; x<=1; x++){
            for(int z=-1; z<=1; z++){
                float3 lattice = float3(x, y, z);
                float3 offset = voronoi_noise_randomVector(g + lattice, AngleOffset);
                float3 v = lattice + offset - f;
                float d = dot(v, v);
                
                if(d < res.x){
                    res.y = res.x;
                    res.x = d;
                    res.z = offset.x;
					Center = (offset + lattice + g) / CellDensity;
                }else if (d < res.y){
                    res.y = d;
					Center = (offset + lattice + g) / CellDensity;
                }
            }
        }
    }
 
    Out = res.x;
    Cells = res.z;
	
}