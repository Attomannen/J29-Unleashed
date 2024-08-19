using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateTerrainDeform : MonoBehaviour
{
	[SerializeField] int explosionTextureIndex = 1;
	[SerializeField] float craterRadius = 5.0f;
	[SerializeField] float craterDepth = 2.0f;

	private void Start()
	{
		if (IsPositionOnTerrain(transform.position))
		{
			StartCoroutine(CreateCraterCoroutine(transform.position, craterRadius, craterDepth));
			StartCoroutine(ChangeTerrainTextureCoroutine(transform.position, craterRadius));
		}
	}

	bool IsPositionOnTerrain(Vector3 position)
	{
		Terrain terrain = Terrain.activeTerrain;
		if (terrain == null) return false;

		TerrainData terrainData = terrain.terrainData;
		Vector3 terrainPos = terrain.transform.position;

		int x = (int)((position.x - terrainPos.x) / terrainData.size.x * terrainData.heightmapResolution);
		int z = (int)((position.z - terrainPos.z) / terrainData.size.z * terrainData.heightmapResolution);

		float terrainHeight = terrain.SampleHeight(position) + terrainPos.y;

		return Mathf.Abs(position.y - terrainHeight) < 10.5f;
	}

	IEnumerator CreateCraterCoroutine(Vector3 position, float radius, float depth)
	{
		Terrain terrain = Terrain.activeTerrain;
		if (terrain == null) yield break;

		TerrainData terrainData = terrain.terrainData;
		Vector3 terrainPos = terrain.transform.position;

		int heightmapWidth = terrainData.heightmapResolution;
		int heightmapHeight = terrainData.heightmapResolution;

		int xBase = (int)((position.x - terrainPos.x) / terrainData.size.x * heightmapWidth);
		int zBase = (int)((position.z - terrainPos.z) / terrainData.size.z * heightmapHeight);
		int craterRadiusInHeightmap = (int)(radius / terrainData.size.x * heightmapWidth);

		int xStart = Mathf.Clamp(xBase - craterRadiusInHeightmap, 0, heightmapWidth);
		int xEnd = Mathf.Clamp(xBase + craterRadiusInHeightmap, 0, heightmapWidth);
		int zStart = Mathf.Clamp(zBase - craterRadiusInHeightmap, 0, heightmapHeight);
		int zEnd = Mathf.Clamp(zBase + craterRadiusInHeightmap, 0, heightmapHeight);

		int width = xEnd - xStart;
		int height = zEnd - zStart;

		float[,] heights = terrainData.GetHeights(xStart, zStart, width, height);

		for (int x = 0; x < width; x++)
		{
			for (int z = 0; z < height; z++)
			{
				int xOffset = x + xStart - xBase + craterRadiusInHeightmap;
				int zOffset = z + zStart - zBase + craterRadiusInHeightmap;
				float distance = Vector2.Distance(new Vector2(xOffset, zOffset), new Vector2(craterRadiusInHeightmap, craterRadiusInHeightmap));
				if (distance < craterRadiusInHeightmap)
				{
					float proportionalDistance = distance / craterRadiusInHeightmap;
					float heightAdjustment = (1 - proportionalDistance) * (depth / terrainData.size.y);
					heights[z, x] -= heightAdjustment;
				}
			}
			if (x % 10 == 0) // Adjust this value to balance between smoothness and performance
			{
				yield return null; // Wait for the next frame
			}
		}

		terrainData.SetHeights(xStart, zStart, heights);
	}

	IEnumerator ChangeTerrainTextureCoroutine(Vector3 position, float radius)
	{
		Terrain terrain = Terrain.activeTerrain;
		if (terrain == null) yield break;

		TerrainData terrainData = terrain.terrainData;
		Vector3 terrainPos = terrain.transform.position;

		int alphaMapWidth = terrainData.alphamapWidth;
		int alphaMapHeight = terrainData.alphamapHeight;

		int xBase = (int)((position.x - terrainPos.x) / terrainData.size.x * alphaMapWidth);
		int zBase = (int)((position.z - terrainPos.z) / terrainData.size.z * alphaMapHeight);
		int craterRadiusInAlphamap = (int)(radius / terrainData.size.x * alphaMapWidth);

		int xStart = Mathf.Max(0, xBase - craterRadiusInAlphamap);
		int zStart = Mathf.Max(0, zBase - craterRadiusInAlphamap);
		int xEnd = Mathf.Min(alphaMapWidth, xBase + craterRadiusInAlphamap);
		int zEnd = Mathf.Min(alphaMapHeight, zBase + craterRadiusInAlphamap);

		int width = xEnd - xStart;
		int height = zEnd - zStart;

		if (width <= 0 || height <= 0)
		{
			yield break;
		}

		float[,,] alphas = terrainData.GetAlphamaps(xStart, zStart, width, height);

		for (int x = 0; x < width; x++)
		{
			for (int z = 0; z < height; z++)
			{
				float distance = Vector2.Distance(new Vector2(x, z), new Vector2(craterRadiusInAlphamap, craterRadiusInAlphamap));
				if (distance < craterRadiusInAlphamap)
				{
					float proportionalDistance = distance / craterRadiusInAlphamap;
					float textureStrength = 1 - proportionalDistance;

					for (int i = 0; i < terrainData.alphamapLayers; i++)
					{
						if (i == explosionTextureIndex)
						{
							alphas[z, x, i] = Mathf.Lerp(alphas[z, x, i], textureStrength, 0.5f);
						}
						else
						{
							alphas[z, x, i] = Mathf.Lerp(alphas[z, x, i], 0, 0.5f);
						}
					}
				}
			}
			if (x % 10 == 0) // Adjust this value to balance between smoothness and performance
			{
				yield return null; // Wait for the next frame
			}
		}

		terrainData.SetAlphamaps(xStart, zStart, alphas);
	}
}
