﻿/*
 * MIT License
 *  Copyright (c) 2018 SPARKCREATIVE
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *  
 *  @author Noriyuki Hiromoto <hrmtnryk@sparkfx.jp>
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SPCRJointDynamicsController))]
public class SPCRJointDynamicsControllerInspector : Editor {
	public enum UpdateJointConnectionType {
		Default,
		SortNearPointXYZ,
		SortNearPointXZ,
		SortNearPointXYZ_FixedBothEnds,
		SortNearPointXZ_FixedBothEnds,
	}

	public int CurrentTool;
	public static readonly GUIContent[] Tools = new GUIContent[] {
		new GUIContent("Basics"),
		new GUIContent("Physics"),
		new GUIContent("Constraints"),
		new GUIContent("Debug"),
		new GUIContent("Conditions"),
		new GUIContent("Tools"),
	};
	float _BoneStretchScale = 1.0f;

	public override void OnInspectorGUI() {
		serializedObject.Update();

		var controller = target as SPCRJointDynamicsController;

		controller.Name = EditorGUILayout.DelayedTextField("名称", controller.Name);

		CurrentTool = GUILayout.Toolbar(CurrentTool, Tools);

		switch (CurrentTool) {
			case 0:
				Titlebar("基本設定", new Color(0.7f, 1.0f, 0.7f));
				controller._RootTransform = (Transform)EditorGUILayout.ObjectField(new GUIContent("親Transform"), controller._RootTransform, typeof(Transform), true);

				if (GUILayout.Button("ルートの点群自動検出", GUILayout.Height(22.0f))) {
					SearchRootPoints(controller);
				}
				EditorGUILayout.PropertyField(serializedObject.FindProperty("_RootPointTable"), new GUIContent($"ルートの点群 ({controller._RootPointTable.Length})"), true);
				GUILayout.Space(5);

				if (GUILayout.Button("Find colliders", GUILayout.Height(22.0f))) {
					SetCollidersInChildren(controller);
				}
				EditorGUILayout.PropertyField(serializedObject.FindProperty("_ColliderTable"), new GUIContent($"コライダー ({controller._ColliderTable.Length})"), true);
				GUILayout.Space(5);

				if (GUILayout.Button("Find grabbers", GUILayout.Height(22.0f))) {
					SetGrabbersInChildren(controller);
				}
				EditorGUILayout.PropertyField(serializedObject.FindProperty("_PointGrabberTable"), new GUIContent($"グラバー ({controller._PointGrabberTable.Length})"), true);
				break;
			case 1:
				Titlebar("物理設定", new Color(0.7f, 1.0f, 0.7f));

				controller._UpdateTiming = (SPCRJointDynamicsController.UpdateTiming)EditorGUILayout.EnumPopup("更新タイミング", controller._UpdateTiming);
				controller._Relaxation = EditorGUILayout.IntSlider("演算繰り返し回数", controller._Relaxation, 1, 16);

				GUILayout.Space(8);
				controller._IgnorePhysicsReset = EditorGUILayout.Toggle("物理リセットを拒否", controller._IgnorePhysicsReset);
				GUILayout.Space(8);
				controller._IsEnableColliderCollision = EditorGUILayout.Toggle("質点とコライダーの衝突判定をする", controller._IsEnableColliderCollision);
				GUILayout.Space(8);
				controller._IsEnableFloorCollision = EditorGUILayout.Toggle("質点と床の衝突判定をする", controller._IsEnableFloorCollision);
				if (controller._IsEnableFloorCollision) {
					controller._FloorHeight = EditorGUILayout.FloatField("床の高さ", controller._FloorHeight);
				}

				GUILayout.Space(8);
				controller._SpringK = EditorGUILayout.Slider("バネ係数", controller._SpringK, 0.0f, 1.0f);

				GUILayout.Space(8);
				controller._Gravity = EditorGUILayout.Vector3Field("重力", controller._Gravity);
				controller._WindForce = EditorGUILayout.Vector3Field("風力", controller._WindForce);

				GUILayout.Space(8);
				controller._MassScaleCurve = EditorGUILayout.CurveField("質量", controller._MassScaleCurve);
				controller._GravityScaleCurve = EditorGUILayout.CurveField("重力", controller._GravityScaleCurve);
				controller._ResistanceCurve = EditorGUILayout.CurveField("空気抵抗", controller._ResistanceCurve);
				controller._FrictionCurve = EditorGUILayout.CurveField("摩擦", controller._FrictionCurve);
				break;
			case 2:
				Titlebar("拘束設定", new Color(0.7f, 1.0f, 0.7f));
				EditorGUILayout.LabelField("=============== 拘束（一括）");
				controller._AllShrinkScaleCurve = EditorGUILayout.CurveField("伸びた時縮む力", controller._AllShrinkScaleCurve);
				controller._AllStretchScaleCurve = EditorGUILayout.CurveField("縮む時伸びる力", controller._AllStretchScaleCurve);
				GUILayout.Space(5);
				EditorGUILayout.LabelField("=============== 構成拘束（垂直）");
				if (controller._IsComputeStructuralVertical) {
					controller._StructuralShrinkVertical = EditorGUILayout.Slider("伸びた時縮む力", controller._StructuralShrinkVertical, 0.0f, 1.0f);
					controller._StructuralStretchVertical = EditorGUILayout.Slider("縮む時伸びる力", controller._StructuralStretchVertical, 0.0f, 1.0f);
					GUILayout.Space(5);
					controller._StructuralShrinkVerticalScaleCurve = EditorGUILayout.CurveField("伸びた時縮む力", controller._StructuralShrinkVerticalScaleCurve);
					controller._StructuralStretchVerticalScaleCurve = EditorGUILayout.CurveField("縮む時伸びる力", controller._StructuralStretchVerticalScaleCurve);
					GUILayout.Space(5);
					controller._IsAllStructuralShrinkVertical = EditorGUILayout.Toggle("伸びた時縮む力（一括設定）", controller._IsAllStructuralShrinkVertical);
					controller._IsAllStructuralStretchVertical = EditorGUILayout.Toggle("縮む時伸びる力（一括設定）", controller._IsAllStructuralStretchVertical);
				} else {
					EditorGUILayout.LabelField("※ 無効 ※");
				}

				EditorGUILayout.LabelField("=============== 構成拘束（水平）");
				if (controller._IsComputeStructuralHorizontal) {
					controller._StructuralShrinkHorizontal = EditorGUILayout.Slider("伸びた時縮む力", controller._StructuralShrinkHorizontal, 0.0f, 1.0f);
					controller._StructuralStretchHorizontal = EditorGUILayout.Slider("縮む時伸びる力", controller._StructuralStretchHorizontal, 0.0f, 1.0f);
					GUILayout.Space(5);
					controller._StructuralShrinkHorizontalScaleCurve = EditorGUILayout.CurveField("伸びた時縮む力", controller._StructuralShrinkHorizontalScaleCurve);
					controller._StructuralStretchHorizontalScaleCurve = EditorGUILayout.CurveField("縮む時伸びる力", controller._StructuralStretchHorizontalScaleCurve);
					GUILayout.Space(5);
					controller._IsAllStructuralShrinkHorizontal = EditorGUILayout.Toggle("伸びた時縮む力（一括設定）", controller._IsAllStructuralShrinkHorizontal);
					controller._IsAllStructuralStretchHorizontal = EditorGUILayout.Toggle("縮む時伸びる力（一括設定）", controller._IsAllStructuralStretchHorizontal);
				} else {
					EditorGUILayout.LabelField("※ 無効 ※");
				}

				EditorGUILayout.LabelField("=============== せん断拘束");
				if (controller._IsComputeShear) {
					controller._ShearShrink = EditorGUILayout.Slider("伸びた時縮む力", controller._ShearShrink, 0.0f, 1.0f);
					controller._ShearStretch = EditorGUILayout.Slider("縮む時伸びる力", controller._ShearStretch, 0.0f, 1.0f);
					GUILayout.Space(5);
					controller._ShearShrinkScaleCurve = EditorGUILayout.CurveField("伸びた時縮む力", controller._ShearShrinkScaleCurve);
					controller._ShearStretchScaleCurve = EditorGUILayout.CurveField("縮む時伸びる力", controller._ShearStretchScaleCurve);
					GUILayout.Space(5);
					controller._IsAllShearShrink = EditorGUILayout.Toggle("伸びた時縮む力（一括設定）", controller._IsAllShearShrink);
					controller._IsAllShearStretch = EditorGUILayout.Toggle("縮む時伸びる力（一括設定）", controller._IsAllShearStretch);
				} else {
					EditorGUILayout.LabelField("※ 無効 ※");
				}

				EditorGUILayout.LabelField("=============== 曲げ拘束（垂直）");
				if (controller._IsComputeBendingVertical) {
					controller._BendingShrinkVertical = EditorGUILayout.Slider("伸びた時縮む力", controller._BendingShrinkVertical, 0.0f, 1.0f);
					controller._BendingStretchVertical = EditorGUILayout.Slider("縮む時伸びる力", controller._BendingStretchVertical, 0.0f, 1.0f);
					GUILayout.Space(5);
					controller._BendingShrinkVerticalScaleCurve = EditorGUILayout.CurveField("伸びた時縮む力", controller._BendingShrinkVerticalScaleCurve);
					controller._BendingStretchVerticalScaleCurve = EditorGUILayout.CurveField("縮む時伸びる力", controller._BendingStretchVerticalScaleCurve);
					GUILayout.Space(5);
					controller._IsAllBendingShrinkVertical = EditorGUILayout.Toggle("伸びた時縮む力（一括設定）", controller._IsAllBendingShrinkVertical);
					controller._IsAllBendingStretchVertical = EditorGUILayout.Toggle("縮む時伸びる力（一括設定）", controller._IsAllBendingStretchVertical);
				} else {
					EditorGUILayout.LabelField("※ 無効 ※");
				}

				EditorGUILayout.LabelField("=============== 曲げ拘束（水平）");
				if (controller._IsComputeBendingHorizontal) {
					controller._BendingShrinkHorizontal = EditorGUILayout.Slider("伸びた時縮む力", controller._BendingShrinkHorizontal, 0.0f, 1.0f);
					controller._BendingStretchHorizontal = EditorGUILayout.Slider("縮む時伸びる力", controller._BendingStretchHorizontal, 0.0f, 1.0f);
					GUILayout.Space(5);
					controller._BendingShrinkHorizontalScaleCurve = EditorGUILayout.CurveField("伸びた時縮む力", controller._BendingShrinkHorizontalScaleCurve);
					controller._BendingStretchHorizontalScaleCurve = EditorGUILayout.CurveField("縮む時伸びる力", controller._BendingStretchHorizontalScaleCurve);
					GUILayout.Space(5);
					controller._IsAllBendingShrinkHorizontal = EditorGUILayout.Toggle("伸びた時縮む力（一括設定）", controller._IsAllBendingShrinkHorizontal);
					controller._IsAllBendingStretchHorizontal = EditorGUILayout.Toggle("縮む時伸びる力（一括設定）", controller._IsAllBendingStretchHorizontal);
				} else {
					EditorGUILayout.LabelField("※ 無効 ※");
				}
				break;
			case 3:
				Titlebar("デバッグ表示", new Color(0.7f, 1.0f, 1.0f));
				EditorGUILayout.BeginHorizontal();
				GUI.color = Color.green;
				controller._IsDebugDraw_StructuralVertical = GUILayout.Toggle(controller._IsDebugDraw_StructuralVertical, "垂直構造", EditorStyles.miniButtonLeft);
				GUI.color = Color.red;
				controller._IsDebugDraw_StructuralHorizontal = GUILayout.Toggle(controller._IsDebugDraw_StructuralHorizontal, "水平構造", EditorStyles.miniButtonRight);
				EditorGUILayout.EndHorizontal();
				GUI.color = Color.white;
				break;
			case 4:
				Titlebar("事前設定", new Color(1.0f, 1.0f, 0.7f));
				controller._WrapHorizontal = GUILayout.Toggle(controller._WrapHorizontal, "拘束のループ", EditorStyles.miniButton);
				GUILayout.Space(5);
				EditorGUILayout.LabelField("拘束の有無");
				EditorGUILayout.BeginHorizontal();
				controller._IsComputeStructuralVertical = GUILayout.Toggle(controller._IsComputeStructuralVertical, "垂直構造", EditorStyles.miniButtonLeft);
				controller._IsComputeStructuralHorizontal = GUILayout.Toggle(controller._IsComputeStructuralHorizontal, "水平構造", EditorStyles.miniButtonMid);
				controller._IsComputeShear = GUILayout.Toggle(controller._IsComputeShear, "せん断", EditorStyles.miniButtonMid);
				controller._IsComputeBendingVertical = GUILayout.Toggle(controller._IsComputeBendingVertical, "垂直曲げ", EditorStyles.miniButtonMid);
				controller._IsComputeBendingHorizontal = GUILayout.Toggle(controller._IsComputeBendingHorizontal, "水平曲げ", EditorStyles.miniButtonRight);
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(5);
				EditorGUILayout.LabelField("衝突（コリジョン）");
				EditorGUILayout.BeginHorizontal();
				controller._IsCollideStructuralVertical = GUILayout.Toggle(controller._IsCollideStructuralVertical, "垂直構造", EditorStyles.miniButtonLeft);
				controller._IsCollideStructuralHorizontal = GUILayout.Toggle(controller._IsCollideStructuralHorizontal, "水平構造", EditorStyles.miniButtonMid);
				controller._IsCollideShear = GUILayout.Toggle(controller._IsCollideShear, "せん断", EditorStyles.miniButtonMid);
				controller._IsCollideBendingVertical = GUILayout.Toggle(controller._IsCollideBendingVertical, "垂直曲げ", EditorStyles.miniButtonMid);
				controller._IsCollideBendingHorizontal = GUILayout.Toggle(controller._IsCollideBendingHorizontal, "水平曲げ", EditorStyles.miniButtonRight);
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(10);

				if (GUILayout.Button("自動設定（現在のルートの点群順序）")) {
					UpdateJointConnection(controller);
				}
				if (GUILayout.Button("自動設定（近ポイント自動検索XYZ）")) {
					SortConstraintsHorizontalRoot(controller, UpdateJointConnectionType.SortNearPointXYZ);
					UpdateJointConnection(controller);
				}
				if (GUILayout.Button("自動設定（近ポイント自動検索XZ）")) {
					SortConstraintsHorizontalRoot(controller, UpdateJointConnectionType.SortNearPointXZ);
					UpdateJointConnection(controller);
				}
				if (GUILayout.Button("自動設定（近ポイント自動検索XYZ：先端終端固定）")) {
					SortConstraintsHorizontalRoot(controller, UpdateJointConnectionType.SortNearPointXYZ_FixedBothEnds);
					UpdateJointConnection(controller);
				}
				if (GUILayout.Button("自動設定（近ポイント自動検索XZ：先端終端固定）")) {
					UpdateJointConnection(controller);
					SortConstraintsHorizontalRoot(controller, UpdateJointConnectionType.SortNearPointXZ_FixedBothEnds);
				}
				if (GUILayout.Button("拘束長さ再計算")) {
					controller.UpdateJointDistance();
				}
				break;
			case 5:
				Titlebar("オプション", new Color(0.7f, 1.0f, 0.7f));

				if (GUILayout.Button("物理初期化")) {
					controller.ResetPhysics(0.3f);
				}

				Titlebar("拡張設定", new Color(1.0f, 0.7f, 0.7f));
				GUILayout.Space(3);
				_BoneStretchScale = EditorGUILayout.Slider("伸縮比率", _BoneStretchScale, -5.0f, +5.0f);
				if (GUILayout.Button("垂直方向にボーンを伸縮する")) {
					controller.StretchBoneLength(_BoneStretchScale);
				}
				break;
		}

		serializedObject.ApplyModifiedProperties();
	}

	private static void UpdateJointConnection(SPCRJointDynamicsController controller) {
		controller.UpdateJointConnection();
		SceneView.RepaintAll();
	}

	private static void Titlebar(string text, Color color) {
		GUILayout.Space(12);

		var backgroundColor = GUI.backgroundColor;
		GUI.backgroundColor = color;

		EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		GUILayout.Label(text);
		EditorGUILayout.EndHorizontal();

		GUI.backgroundColor = backgroundColor;

		GUILayout.Space(3);
	}

	private static void SearchRootPoints(Transform transform, List<SPCRJointDynamicsPoint> list) {
		for (int i = 0; i < transform.childCount; ++i) {
			var child = transform.GetChild(i);
			var point = child.GetComponent<SPCRJointDynamicsPoint>();
			if (point != null) {
				list.Add(point);
			} else {
				SearchRootPoints(child, list);
			}
		}
	}
	private static void SearchRootPoints(SPCRJointDynamicsController controller) {
		if (controller._RootTransform == null) {
			controller._RootTransform = controller.transform;
		}
		var PointList = new List<SPCRJointDynamicsPoint>();
		SearchRootPoints(controller._RootTransform, PointList);
		controller._RootPointTable = PointList.ToArray();
	}
	private static void SetCollidersInChildren(SPCRJointDynamicsController controller) {
		if (controller._RootTransform != null) {
			controller._ColliderTable = controller._RootTransform.GetComponentsInChildren<SPCRJointDynamicsCollider>();
		}
	}
	private static void SetGrabbersInChildren(SPCRJointDynamicsController controller) {
		if (controller._RootTransform != null) {
			controller._PointGrabberTable = controller._RootTransform.GetComponentsInChildren<SPCRJointDynamicsPointGrabber>();
		}
	}

	private static SPCRJointDynamicsPoint PopNearestPoint(Vector3 Base, List<SPCRJointDynamicsPoint> Source, bool IsIgnoreY) {
		var NearestDistanceSquared = float.MaxValue;
		var NearestIndex = -1;
		for (int i = 0; i < Source.Count; ++i) {
			var Direction = Source[i].transform.position - Base;
			if (IsIgnoreY)
				Direction.y = 0.0f;
			var DistanceSquared = Direction.sqrMagnitude;
			if (NearestDistanceSquared > DistanceSquared) {
				NearestDistanceSquared = DistanceSquared;
				NearestIndex = i;
			}
		}
		var Point = Source[NearestIndex];
		Source.RemoveAt(NearestIndex);
		return Point;
	}
	private static void SortConstraintsHorizontalRoot(SPCRJointDynamicsController controller, UpdateJointConnectionType Type) {
		switch (Type) {
			case UpdateJointConnectionType.Default: {
				}
				break;
			case UpdateJointConnectionType.SortNearPointXYZ: {
					var SourcePoints = new List<SPCRJointDynamicsPoint>();
					for (int i = 1; i < controller._RootPointTable.Length; ++i) {
						SourcePoints.Add(controller._RootPointTable[i]);
					}
					var SortedPoints = new List<SPCRJointDynamicsPoint> {
						controller._RootPointTable[0]
					};
					while (SourcePoints.Count > 0) {
						SortedPoints.Add(PopNearestPoint(
							SortedPoints[SortedPoints.Count - 1].transform.position,
							SourcePoints,
							false));
					}
					controller._RootPointTable = SortedPoints.ToArray();
				}
				break;
			case UpdateJointConnectionType.SortNearPointXZ: {
					var SourcePoints = new List<SPCRJointDynamicsPoint>();
					for (int i = 1; i < controller._RootPointTable.Length; ++i) {
						SourcePoints.Add(controller._RootPointTable[i]);
					}
					var SortedPoints = new List<SPCRJointDynamicsPoint> {
						controller._RootPointTable[0]
					};
					while (SourcePoints.Count > 0) {
						SortedPoints.Add(PopNearestPoint(
							SortedPoints[SortedPoints.Count - 1].transform.position,
							SourcePoints,
							true));
					}
					controller._RootPointTable = SortedPoints.ToArray();
				}
				break;
			case UpdateJointConnectionType.SortNearPointXYZ_FixedBothEnds: {
					var SourcePoints = new List<SPCRJointDynamicsPoint>();
					var EdgeB = controller._RootPointTable[controller._RootPointTable.Length - 1];
					for (int i = 1; i < controller._RootPointTable.Length - 1; ++i) {
						SourcePoints.Add(controller._RootPointTable[i]);
					}
					var SortedPoints = new List<SPCRJointDynamicsPoint> {
						controller._RootPointTable[0]
					};
					while (SourcePoints.Count > 0) {
						SortedPoints.Add(PopNearestPoint(
							SortedPoints[SortedPoints.Count - 1].transform.position,
							SourcePoints,
							false));
					}
					SortedPoints.Add(EdgeB);
					controller._RootPointTable = SortedPoints.ToArray();
				}
				break;
			case UpdateJointConnectionType.SortNearPointXZ_FixedBothEnds: {
					var SourcePoints = new List<SPCRJointDynamicsPoint>();
					var EdgeB = controller._RootPointTable[controller._RootPointTable.Length - 1];
					for (int i = 1; i < controller._RootPointTable.Length - 1; ++i) {
						SourcePoints.Add(controller._RootPointTable[i]);
					}
					var SortedPoints = new List<SPCRJointDynamicsPoint> {
						controller._RootPointTable[0]
					};
					while (SourcePoints.Count > 0) {
						SortedPoints.Add(PopNearestPoint(
							SortedPoints[SortedPoints.Count - 1].transform.position,
							SourcePoints,
							true));
					}
					SortedPoints.Add(EdgeB);
					controller._RootPointTable = SortedPoints.ToArray();
				}
				break;
		}
	}
}

[InitializeOnLoad]
public class SPCRJointDynamicsSettingsWindow : EditorWindow {
	static SPCRJointDynamicsSettingsWindow _Window;

	static SPCRJointDynamicsSettingsWindow() {
		EditorApplication.update += Update;
	}

	static void Update() {
		if (!EditorApplication.isPlayingOrWillChangePlaymode) {
			if (!PlayerSettings.allowUnsafeCode || (PlayerSettings.scriptingRuntimeVersion != ScriptingRuntimeVersion.Latest)) {
				_Window = GetWindow<SPCRJointDynamicsSettingsWindow>(true);
				_Window.minSize = new Vector2(450, 200);
			}
		}
	}

	void OnGUI() {
		EditorGUILayout.HelpBox(
			"Recommended project settings for SPCRJointDynamics:\n" +
			"PlayerSettings.allowUnsafeCode = true\n" +
			"PlayerSettings.scriptingRuntimeVersion = Latest",
			MessageType.Info);
		if (GUILayout.Button("fix Settings")) {
			if (!PlayerSettings.allowUnsafeCode) {
				PlayerSettings.allowUnsafeCode = true;
			}
			if (PlayerSettings.scriptingRuntimeVersion != ScriptingRuntimeVersion.Latest) {
				PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;
			}

			Close();
		}
	}
}
