﻿using System;
using System.Collections.Generic;
using System.Linq;
using TriangleNet.Meshing;
using TriangleNet.Topology;
using UnityEngine;
using UnityEngine.Rendering;
using Visualizer.MapEditor;

/// <summary>
/// メッシュの生成結果
/// </summary>
public class M_GenerateContext
{
    // 生成されたオブジェクト
    public readonly GameObject GeneratedObject;
    
    // 三角形の頂点情報
    // これを使ってVerticesで三角形を作る
    public readonly List<(Vector2 v0, Vector2 v1, Vector2 v2)> Triangles;
    
    // 三角形の重心
    // これは使い方がわからん
    public readonly List<Vector2> Centroids;
    
    // グリッドマップデータ
    public readonly MapData MapData;

    public M_GenerateContext(GameObject generatedObject, List<(Vector2 v0, Vector2 v1, Vector2 v2)> triangles, List<Vector2> centroids, MapData mapData)
    {
        GeneratedObject = generatedObject;
        Triangles = triangles;
        Centroids = centroids;
        MapData = mapData;
    }
}

public class M_Starter : MonoBehaviour
{
    [SerializeField] private M_MapDataManager mapDataManager;
    [SerializeField] private Material material;
    [SerializeField] private Vector2 displaySize;
    [SerializeField] private MeshTest meshtest;

    // gridDataがどうなってるかを可視化するための作業
    [SerializeField]
    List<ChildArray> watchGrids;

    public event Action<M_GenerateContext> OnMeshGenerated;

    private GridTriangulator triangulator;
    private List<Vector2> trianglePoints = new List<Vector2>();


    private void Awake()
    {
        OnMeshGenerated += meshtest.DeleteBlockMesh;
    }
    private void Start()
    {
        MapData mapData = mapDataManager.Load();
        // マップを上下反転する処理が行われている
        // ↑左右も反転してるっぽい？
        triangulator = new GridTriangulator(mapData);

        // gridDataがどうなってるかを可視化するための作業
        watchGrids = triangulator.GetWatchgridData();

        // メッシュの作成
        (GameObject gameObj, IMesh tMesh) polygonObject = CreatePolygonObject();

        // マテリアルの設定
        var meshRenderer = polygonObject.gameObj.GetComponent<MeshRenderer>();
        meshRenderer.material = material;

        // 表示サイズの設定
        var displayScaler = polygonObject.gameObj.AddComponent<DisplayScaler>();
        displayScaler.Scale(displaySize, new Vector2(mapData.Width, mapData.Height));

        // 
        var triangles = CreateTrianglePoints(polygonObject.tMesh);
        // 重心のリスト
        var centroids = triangles.Select(triangle => (triangle.v0 + triangle.v1 + triangle.v2) / 3).ToList();

        // 障害物のメッシュを消す処理
        OnMeshGenerated?.Invoke(new M_GenerateContext(polygonObject.gameObj, triangles, centroids, mapData));

        Transform scaler = displayScaler.transform;
        trianglePoints = centroids.Select(triangle => triangle * (Vector2)scaler.localScale + (Vector2)scaler.localPosition).ToList();
    }

    private List<(Vector2 v0, Vector2 v1, Vector2 v2)> CreateTrianglePoints(IMesh tMesh)
    {
        var points = new List<(Vector2 v0, Vector2 v1, Vector2 v2)>();

        foreach (Triangle triangle in tMesh.Triangles)
        {
            var v0 = triangle.GetVertex(0);
            var v1 = triangle.GetVertex(1);
            var v2 = triangle.GetVertex(2);

            var p0 = new Vector3((float)v0.X, (float)v0.Y);
            var p1 = new Vector3((float)v1.X, (float)v1.Y);
            var p2 = new Vector3((float)v2.X, (float)v2.Y);

            points.Add((p0, p1, p2));
        }

        return points;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (Vector3 trianglePoint in trianglePoints)
        {
            Gizmos.DrawSphere(trianglePoint, 0.02f);
        }
    }

    private (GameObject gameObj, IMesh tMesh) CreatePolygonObject()
    {
        GameObject triangleMesh = new GameObject("TriangleMesh");

        Mesh mesh = new Mesh();

        //メッシュを三角ポリゴンに分割する
        IMesh tMesh = triangulator.Triangulate(mesh);

        MeshFilter mf = triangleMesh.AddComponent<MeshFilter>();
        MeshRenderer renderer = triangleMesh.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        mf.mesh = mesh;

        return (triangleMesh, tMesh);
    }
}