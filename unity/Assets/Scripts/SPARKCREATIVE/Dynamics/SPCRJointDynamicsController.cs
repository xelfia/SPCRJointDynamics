/*
 * MIT License
 *  Copyright (c) 2018 SPARKCREATIVE
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *  
 *  @author Noriyuki Hiromoto <hrmtnryk@sparkfx.jp>
*/

using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class SPCRJointDynamicsController : MonoBehaviour {
	public enum ConstraintType {
		Structural_Vertical,
		Structural_Horizontal,
		Shear,
		Bending_Vertical,
		Bending_Horizontal,
	}

	public enum UpdateTiming {
		LateUpdate,
		FixedUpdate,
	}

	[Serializable]
	public class SPCRJointDynamicsConstraint {
		public ConstraintType _Type;
		public SPCRJointDynamicsPoint _PointA;
		public SPCRJointDynamicsPoint _PointB;
		public float _Length;

		public void UpdateLength() => _Length = Length;
		public float Length => Vector3.Distance(_PointA.transform.position, _PointB.transform.position);
		public SPCRJointDynamicsConstraint(ConstraintType Type, SPCRJointDynamicsPoint PointA, SPCRJointDynamicsPoint PointB)
			=> (_Type, _PointA, _PointB) = (Type, PointA, PointB);
		public static SPCRJointDynamicsConstraint Create(
			ConstraintType Type, SPCRJointDynamicsPoint PointA, SPCRJointDynamicsPoint PointB) {
			var result = new SPCRJointDynamicsConstraint(Type, PointA, PointB);
			result.UpdateLength();
			return result;
		}
	}

	public string Name;

	public Transform _RootTransform;
	public SPCRJointDynamicsPoint[] _RootPointTable = new SPCRJointDynamicsPoint[0];

	public SPCRJointDynamicsCollider[] _ColliderTable = new SPCRJointDynamicsCollider[0];

	public UpdateTiming _UpdateTiming = UpdateTiming.LateUpdate;

	public int _Relaxation = 3;

	public bool _IsEnableFloorCollision = true;
	public float _FloorHeight = 0.02f;

	public bool _IsEnableColliderCollision = false;

	public bool _IgnorePhysicsReset = false;

	public AnimationCurve _MassScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _GravityScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _ResistanceCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 0.0f) });
	public AnimationCurve _FrictionCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 0.7f), new Keyframe(1.0f, 0.7f) });

	public AnimationCurve _AllShrinkScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _AllStretchScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _StructuralShrinkVerticalScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _StructuralStretchVerticalScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _StructuralShrinkHorizontalScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _StructuralStretchHorizontalScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _ShearShrinkScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _ShearStretchScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _BendingShrinkVerticalScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _BendingStretchVerticalScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _BendingShrinkHorizontalScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
	public AnimationCurve _BendingStretchHorizontalScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });

	public Vector3 _Gravity = new Vector3(0.0f, -10.0f, 0.0f);
	public Vector3 _WindForce = new Vector3(0.0f, 0.0f, 0.0f);

	public float _SpringK = 1.0f;

	public float _StructuralShrinkVertical = 1.0f;
	public float _StructuralStretchVertical = 1.0f;
	public float _StructuralShrinkHorizontal = 1.0f;
	public float _StructuralStretchHorizontal = 1.0f;
	public float _ShearShrink = 1.0f;
	public float _ShearStretch = 1.0f;
	public float _BendingShrinkVertical = 1.0f;
	public float _BendingStretchVertical = 1.0f;
	public float _BendingShrinkHorizontal = 1.0f;
	public float _BendingStretchHorizontal = 1.0f;

	public bool _IsAllStructuralShrinkVertical = false;
	public bool _IsAllStructuralStretchVertical = true;
	public bool _IsAllStructuralShrinkHorizontal = true;
	public bool _IsAllStructuralStretchHorizontal = true;
	public bool _IsAllShearShrink = true;
	public bool _IsAllShearStretch = true;
	public bool _IsAllBendingShrinkVertical = true;
	public bool _IsAllBendingStretchVertical = true;
	public bool _IsAllBendingShrinkHorizontal = true;
	public bool _IsAllBendingStretchHorizontal = true;

	public bool _IsCollideStructuralVertical = true;
	public bool _IsCollideStructuralHorizontal = true;
	public bool _IsCollideShear = true;
	public bool _IsCollideBendingVertical = false;
	public bool _IsCollideBendingHorizontal = false;

	[SerializeField]
	private SPCRJointDynamicsPoint[] _PointTable = new SPCRJointDynamicsPoint[0];
	[SerializeField]
	private SPCRJointDynamicsConstraint[] _ConstraintsStructuralVertical = new SPCRJointDynamicsConstraint[0];
	[SerializeField]
	private SPCRJointDynamicsConstraint[] _ConstraintsStructuralHorizontal = new SPCRJointDynamicsConstraint[0];
	[SerializeField]
	private SPCRJointDynamicsConstraint[] _ConstraintsShear = new SPCRJointDynamicsConstraint[0];
	[SerializeField]
	private SPCRJointDynamicsConstraint[] _ConstraintsBendingVertical = new SPCRJointDynamicsConstraint[0];
	[SerializeField]
	private SPCRJointDynamicsConstraint[] _ConstraintsBendingHorizontal = new SPCRJointDynamicsConstraint[0];

	public bool _WrapHorizontal = false;
	public bool _IsComputeStructuralVertical = true;
	public bool _IsComputeStructuralHorizontal = true;
	public bool _IsComputeShear = false;
	public bool _IsComputeBendingVertical = true;
	public bool _IsComputeBendingHorizontal = true;

	public bool _IsDebugDraw_StructuralVertical = false;
	public bool _IsDebugDraw_StructuralHorizontal = false;

	[SerializeField]
	private SPCRJointDynamicsJob.Constraint[][] _ConstraintTable;

	[SerializeField]
	private int _MaxPointDepth = 0;

	private float _WindTime;
	public float WindFrequency = 3.0f;
	private float _Delay;

	private readonly SPCRJointDynamicsJob _Job = new SPCRJointDynamicsJob();

	public void Refresh() {
		OnDestroy();
		Awake();
	}

	private void Awake() {
		var PointTransforms = new Transform[_PointTable.Length];
		var Points = new SPCRJointDynamicsJob.Point[_PointTable.Length];
		for (int i = 0; i < _PointTable.Length; ++i) {
			var src = _PointTable[i];
			var rate = src._Depth / _MaxPointDepth;

			PointTransforms[i] = src.transform;

			Points[i].Parent = -1;
			Points[i].Child = -1;
			Points[i].Weight = src._IsFixed ? 0.0f : 1.0f;
			Points[i].Mass = src._Mass * _MassScaleCurve.Evaluate(rate);
			Points[i].Resistance = 1.0f - _ResistanceCurve.Evaluate(rate);
			Points[i].Gravity = _Gravity * _GravityScaleCurve.Evaluate(rate);
			Points[i].FrictionScale = _FrictionCurve.Evaluate(rate);
			Points[i].BoneAxis = src._BoneAxis;
			Points[i].Position = PointTransforms[i].position;
			Points[i].OldPosition = PointTransforms[i].position;
			Points[i].InitialPosition = _RootTransform.InverseTransformPoint(PointTransforms[i].position);
			Points[i].PreviousDirection = PointTransforms[i].parent.position - PointTransforms[i].position;
			Points[i].ParentLength = Points[i].PreviousDirection.magnitude;
			Points[i].LocalRotation = PointTransforms[i].localRotation;

			Points[i].StructuralShrinkVertical = _StructuralShrinkVerticalScaleCurve.Evaluate(rate);
			Points[i].StructuralStretchVertical = _StructuralStretchVerticalScaleCurve.Evaluate(rate);
			Points[i].StructuralShrinkHorizontal = _StructuralShrinkHorizontalScaleCurve.Evaluate(rate);
			Points[i].StructuralStretchHorizontal = _StructuralStretchHorizontalScaleCurve.Evaluate(rate);
			Points[i].ShearShrink = _ShearShrinkScaleCurve.Evaluate(rate);
			Points[i].ShearStretch = _ShearStretchScaleCurve.Evaluate(rate);
			Points[i].BendingShrinkVertical = _BendingShrinkVerticalScaleCurve.Evaluate(rate);
			Points[i].BendingStretchVertical = _BendingStretchVerticalScaleCurve.Evaluate(rate);
			Points[i].BendingShrinkHorizontal = _BendingShrinkHorizontalScaleCurve.Evaluate(rate);
			Points[i].BendingStretchHorizontal = _BendingStretchHorizontalScaleCurve.Evaluate(rate);

			var AllShrinkScale = _AllShrinkScaleCurve.Evaluate(rate);
			var AllStretchScale = _AllStretchScaleCurve.Evaluate(rate);
			if (_IsAllStructuralShrinkVertical)
				Points[i].StructuralShrinkVertical *= AllShrinkScale;
			if (_IsAllStructuralStretchVertical)
				Points[i].StructuralStretchVertical *= AllStretchScale;
			if (_IsAllStructuralShrinkHorizontal)
				Points[i].StructuralShrinkHorizontal *= AllShrinkScale;
			if (_IsAllStructuralStretchHorizontal)
				Points[i].StructuralStretchHorizontal *= AllStretchScale;
			if (_IsAllShearShrink)
				Points[i].ShearShrink *= AllShrinkScale;
			if (_IsAllShearStretch)
				Points[i].ShearStretch *= AllStretchScale;
			if (_IsAllBendingShrinkVertical)
				Points[i].BendingShrinkVertical *= AllShrinkScale;
			if (_IsAllBendingStretchVertical)
				Points[i].BendingStretchVertical *= AllStretchScale;
			if (_IsAllBendingShrinkHorizontal)
				Points[i].BendingShrinkHorizontal *= AllShrinkScale;
			if (_IsAllBendingStretchHorizontal)
				Points[i].BendingStretchHorizontal *= AllStretchScale;
		}

		for (int i = 0; i < _PointTable.Length; ++i) {
			if (_PointTable[i]._RefChildPoint == null)
				continue;

			Points[i].Child = _PointTable[i]._RefChildPoint._Index;
			Points[Points[i].Child].Parent = _PointTable[i]._Index;
		}

		CreateConstraintTable();
		_Job.Initialize(_RootTransform, Points, PointTransforms, _ConstraintTable, _ColliderTable);

		_Delay = 1.0f / 20.0f;
	}

	private void OnDestroy() {
		_Job.Uninitialize();
	}

	private void FixedUpdate() {
		if (_UpdateTiming != UpdateTiming.FixedUpdate)
			return;
		UpdateInternal(Time.fixedDeltaTime);
	}
	private void LateUpdate() {
		if (_UpdateTiming != UpdateTiming.LateUpdate)
			return;
		UpdateInternal(Time.deltaTime);
	}

	private void UpdateInternal(float DeltaTime) {
		if (_Delay > 0.0f) {
			_Delay -= DeltaTime;
			if (_Delay > 0.0f) {
				return;
			}

			_Job.Reset();
		}

		var StepTime = DeltaTime;
		var WindForcePower = Mathf.Sin(_WindTime) * 0.5f + 0.5f;
		_WindTime += StepTime * WindFrequency;

		_Job.Execute(
			StepTime, _WindForce * WindForcePower,
			_Relaxation, _SpringK,
			_IsEnableFloorCollision, _FloorHeight,
			_IsEnableColliderCollision);
	}
	private void CreateConstraintStructuralVertical(SPCRJointDynamicsPoint Point, List<SPCRJointDynamicsConstraint> Result) {
		for (int i = 0; i < Point.transform.childCount; ++i) {
			var child = Point.transform.GetChild(i);
			var child_point = child.GetComponent<SPCRJointDynamicsPoint>();
			if (child_point != null) {
				Point._RefChildPoint = child_point;
				var LocalPosition = Point.transform.InverseTransformPoint(Point._RefChildPoint.transform.position);
				Point._BoneAxis = LocalPosition.normalized;

				var Constraint = SPCRJointDynamicsConstraint.Create(
					 ConstraintType.Structural_Vertical, Point, child_point);
				Result.Add(Constraint);

				CreateConstraintStructuralVertical(child_point, Result);
			}
		}
	}
	private void ComputePointParameter(SPCRJointDynamicsPoint Point, int Depth) {
		_MaxPointDepth = Mathf.Max(_MaxPointDepth, Depth);

		Point._Depth = Depth;
		Point._IsFixed = Point._Depth == 0;

		for (int i = 0; i < Point.transform.childCount; ++i) {
			var ChildPoint = Point.transform.GetChild(i).GetComponent<SPCRJointDynamicsPoint>();
			if (ChildPoint != null) {
				ComputePointParameter(ChildPoint, Depth + 1);
			}
		}
	}

	public void UpdateJointConnection() {
		var PointAll = new List<SPCRJointDynamicsPoint>();
		foreach (var root in _RootPointTable) {
			PointAll.AddRange(root.GetComponentsInChildren<SPCRJointDynamicsPoint>());
		}
		_PointTable = PointAll.ToArray();
		for (int i = 0; i < _PointTable.Length; ++i) {
			_PointTable[i]._Index = i;
		}

		// All Points
		var HorizontalRootCount = _RootPointTable.Length;

		// Compute Each Point Parameter
		{
			_MaxPointDepth = 0;
			for (int i = 0; i < HorizontalRootCount; ++i) {
				ComputePointParameter(_RootPointTable[i], 0);
			}
		}

		// Vertical Structural
		_ConstraintsStructuralVertical = new SPCRJointDynamicsConstraint[0];
		{
			var ConstraintList = new List<SPCRJointDynamicsConstraint>();
			for (int i = 0; i < HorizontalRootCount; ++i) {
				CreateConstraintStructuralVertical(_RootPointTable[i], ConstraintList);
			}
			_ConstraintsStructuralVertical = ConstraintList.ToArray();
		}

		// Stractural Horizontal
		_ConstraintsStructuralHorizontal = new SPCRJointDynamicsConstraint[0];
		{
			var ConstraintList = new List<SPCRJointDynamicsConstraint>();
			if (_WrapHorizontal) {
				for (int i = 0; i < HorizontalRootCount; ++i) {
					CreateConstraintHorizontal(
						_RootPointTable[i],
						_RootPointTable[(i + 1) % HorizontalRootCount],
						ConstraintList);
				}
			} else {
				for (int i = 0; i < HorizontalRootCount - 1; ++i) {
					CreateConstraintHorizontal(
						_RootPointTable[i + 0],
						_RootPointTable[i + 1],
						ConstraintList);
				}
			}
			_ConstraintsStructuralHorizontal = ConstraintList.ToArray();
		}

		// Shear
		_ConstraintsShear = new SPCRJointDynamicsConstraint[0];
		{
			var ConstraintList = new List<SPCRJointDynamicsConstraint>();
			if (_WrapHorizontal) {
				for (int i = 0; i < HorizontalRootCount; ++i) {
					CreateConstraintShear(
						_RootPointTable[i],
						_RootPointTable[(i + 1) % HorizontalRootCount],
						ConstraintList);
				}
			} else {
				for (int i = 0; i < HorizontalRootCount - 1; ++i) {
					CreateConstraintShear(
						_RootPointTable[i + 0],
						_RootPointTable[i + 1],
						ConstraintList);
				}
			}
			_ConstraintsShear = ConstraintList.ToArray();
		}

		// Bending Vertical
		_ConstraintsBendingVertical = new SPCRJointDynamicsConstraint[0];
		{
			var ConstraintList = new List<SPCRJointDynamicsConstraint>();
			for (int i = 0; i < HorizontalRootCount; ++i) {
				CreateConstraintBendingVertical(
					_RootPointTable[i],
					ConstraintList);
			}
			_ConstraintsBendingVertical = ConstraintList.ToArray();
		}

		// Bending Horizontal
		_ConstraintsBendingHorizontal = new SPCRJointDynamicsConstraint[0];
		{
			var ConstraintList = new List<SPCRJointDynamicsConstraint>();
			if (_WrapHorizontal) {
				for (int i = 0; i < HorizontalRootCount; ++i) {
					CreateConstraintBendingHorizontal(
						_RootPointTable[i],
						_RootPointTable[(i + 2) % HorizontalRootCount],
						ConstraintList);
				}
			} else {
				for (int i = 0; i < HorizontalRootCount - 2; ++i) {
					CreateConstraintBendingHorizontal(
						_RootPointTable[i + 0],
						_RootPointTable[i + 2],
						ConstraintList);
				}
			}
			_ConstraintsBendingHorizontal = ConstraintList.ToArray();
		}

		CreateConstraintTable();
	}

	public void UpdateJointDistance() {
		for (int i = 0; i < _ConstraintsStructuralVertical.Length; ++i) {
			_ConstraintsStructuralVertical[i].UpdateLength();
		}
		for (int i = 0; i < _ConstraintsStructuralHorizontal.Length; ++i) {
			_ConstraintsStructuralHorizontal[i].UpdateLength();
		}
		for (int i = 0; i < _ConstraintsShear.Length; ++i) {
			_ConstraintsShear[i].UpdateLength();
		}
		for (int i = 0; i < _ConstraintsBendingVertical.Length; ++i) {
			_ConstraintsBendingVertical[i].UpdateLength();
		}
		for (int i = 0; i < _ConstraintsBendingHorizontal.Length; ++i) {
			_ConstraintsBendingHorizontal[i].UpdateLength();
		}
	}

	public void ResetPhysics(float Delay) {
		if (_IgnorePhysicsReset)
			return;

		_Job.Restore();
		_Delay = Delay;
	}
	private static SPCRJointDynamicsPoint GetChildJointDynamicsPoint(SPCRJointDynamicsPoint Parent) {
		if (Parent != null) {
			for (int i = 0; i < Parent.transform.childCount; ++i) {
				var child = Parent.transform.GetChild(i).GetComponent<SPCRJointDynamicsPoint>();
				if (child != null) {
					return child;
				}
			}
		}
		return null;
	}
	private void CreateConstraintHorizontal(
		SPCRJointDynamicsPoint PointA,
		SPCRJointDynamicsPoint PointB,
		List<SPCRJointDynamicsConstraint> Result) {
		if (!ValidPair(PointA, PointB))
			return;

		var childPointA = GetChildJointDynamicsPoint(PointA);
		var childPointB = GetChildJointDynamicsPoint(PointB);

		var flags = (childPointA != null ? 1 : 0) + (childPointB != null ? 2 : 0);
		switch (flags) {
			case 1:
				Result.Add(SPCRJointDynamicsConstraint.Create(
					ConstraintType.Structural_Horizontal, childPointA, PointB));
				break;
			case 2:
				Result.Add(SPCRJointDynamicsConstraint.Create(
					ConstraintType.Structural_Horizontal, PointA, childPointB));
				break;
			case 3:
				Result.Add(SPCRJointDynamicsConstraint.Create(
					ConstraintType.Structural_Horizontal, childPointA, childPointB));
				CreateConstraintHorizontal(childPointA, childPointB, Result);
				break;
		}
	}
	private void CreateConstraintShear(
		SPCRJointDynamicsPoint PointA,
		SPCRJointDynamicsPoint PointB,
		List<SPCRJointDynamicsConstraint> Result) {
		if (!ValidPair(PointA, PointB))
			return;

		var childPointA = GetChildJointDynamicsPoint(PointA);
		var childPointA2 = GetChildJointDynamicsPoint(childPointA);
		var childPointA3 = GetChildJointDynamicsPoint(childPointA2);

		if (childPointA != null) {
			Result.Add(SPCRJointDynamicsConstraint.Create(
				ConstraintType.Shear, childPointA, PointB));
		} else if (childPointA2 != null) {
			Result.Add(SPCRJointDynamicsConstraint.Create(
				ConstraintType.Shear, childPointA2, PointB));
		} else if (childPointA3 != null) {
			Result.Add(SPCRJointDynamicsConstraint.Create(
				ConstraintType.Shear, childPointA3, PointB));
		}

		var childPointB = GetChildJointDynamicsPoint(PointB);
		var childPointB2 = GetChildJointDynamicsPoint(childPointB);
		var childPointB3 = GetChildJointDynamicsPoint(childPointB2);

		if (childPointB != null) {
			Result.Add(SPCRJointDynamicsConstraint.Create(
				ConstraintType.Shear, PointA, childPointB));
		} else if (childPointB2 != null) {
			Result.Add(SPCRJointDynamicsConstraint.Create(
				ConstraintType.Shear, PointA, childPointB2));
		} else if (childPointB3 != null) {
			Result.Add(SPCRJointDynamicsConstraint.Create(
				ConstraintType.Shear, PointA, childPointB3));
		}
		CreateConstraintShear(childPointA, childPointB, Result);
	}
	private void CreateConstraintBendingVertical(
		SPCRJointDynamicsPoint Point,
		List<SPCRJointDynamicsConstraint> Result) {
		var childA = OnlyChild(Point);
		if (childA == null)
			return;
		var childPointB = OnlyChildPoint(childA);
		if (childPointB != null) {
			Result.Add(SPCRJointDynamicsConstraint.Create(
				ConstraintType.Bending_Vertical, Point, childPointB));
		}
		var childPointA = childA.GetComponent<SPCRJointDynamicsPoint>();
		if (childPointA != null) {
			CreateConstraintBendingVertical(childPointA, Result);
		}
	}
	private static Transform OnlyChild(SPCRJointDynamicsPoint Point)
		=> Point.transform.childCount != 1
		? null
		: Point.transform.GetChild(0);
	private static SPCRJointDynamicsPoint OnlyChildPoint(Transform Transform)
		=> Transform.childCount != 1
		? null
		: Transform.GetChild(0).GetComponent<SPCRJointDynamicsPoint>();

	private void CreateConstraintBendingHorizontal(
		SPCRJointDynamicsPoint PointA,
		SPCRJointDynamicsPoint PointB,
		List<SPCRJointDynamicsConstraint> Result) {
		if (!ValidPair(PointA, PointB))
			return;

		var childPointA = GetChildJointDynamicsPoint(PointA);
		var childPointB = GetChildJointDynamicsPoint(PointB);

		var flags = (childPointA != null ? 1 : 0) + (childPointB != null ? 2 : 0);
		switch (flags) {
			case 1:
				Result.Add(SPCRJointDynamicsConstraint.Create(
					ConstraintType.Bending_Horizontal, childPointA, PointB));
				break;
			case 2:
				Result.Add(SPCRJointDynamicsConstraint.Create(
					ConstraintType.Bending_Horizontal, PointA, childPointB));
				break;
			case 3:
				Result.Add(SPCRJointDynamicsConstraint.Create(
					ConstraintType.Bending_Horizontal, childPointA, childPointB));
				CreateConstraintHorizontal(childPointA, childPointB, Result);
				break;
		}
	}
	private static bool ValidPair(SPCRJointDynamicsPoint PointA, SPCRJointDynamicsPoint PointB)
		=> PointA != null && PointB != null && PointA != PointB;
	private static bool ContainsEqualIndex(List<SPCRJointDynamicsJob.Constraint> list, SPCRJointDynamicsJob.Constraint constraint) {
		for (int i = 0; i < list.Count; ++i) {
			if (list[i].IndexA == constraint.IndexA ||
				list[i].IndexA == constraint.IndexB ||
				list[i].IndexB == constraint.IndexA ||
				list[i].IndexB == constraint.IndexB)
				return true;
		}
		return false;
	}

	private static void PushConstraintTable(List<List<SPCRJointDynamicsJob.Constraint>> ListTable, SPCRJointDynamicsJob.Constraint constraint) {
		for (int i = 0; i < ListTable.Count; ++i) {
			var table = ListTable[i];
			if (!ContainsEqualIndex(table, constraint)) {
				table.Add(constraint);
				return;
			}
		}
		ListTable.Add(new List<SPCRJointDynamicsJob.Constraint> { constraint });
	}
	private void CreateConstraintTable() {
		var ConstraintTable = new List<List<SPCRJointDynamicsJob.Constraint>>();

		if (_IsComputeBendingHorizontal) {
			foreach (var src in _ConstraintsBendingHorizontal) {
				var c = new SPCRJointDynamicsJob.Constraint {
					Type = src._Type,
					IndexA = src._PointA._Index,
					IndexB = src._PointB._Index,
					Length = src._Length,
					Shrink = _BendingShrinkHorizontal,
					Stretch = _BendingStretchHorizontal,
					IsCollision = !src._PointA._IsFixed && !src._PointB._IsFixed && _IsCollideBendingHorizontal ? 1 : 0
				};
				PushConstraintTable(ConstraintTable, c);
			}
		}
		if (_IsComputeStructuralHorizontal) {
			foreach (var src in _ConstraintsStructuralHorizontal) {
				var c = new SPCRJointDynamicsJob.Constraint {
					Type = src._Type,
					IndexA = src._PointA._Index,
					IndexB = src._PointB._Index,
					Length = src._Length,
					Shrink = _StructuralShrinkHorizontal,
					Stretch = _StructuralStretchHorizontal,
					IsCollision = !src._PointA._IsFixed && !src._PointB._IsFixed && _IsCollideStructuralHorizontal ? 1 : 0
				};
				PushConstraintTable(ConstraintTable, c);
			}
		}
		if (_IsComputeShear) {
			foreach (var src in _ConstraintsShear) {
				var c = new SPCRJointDynamicsJob.Constraint {
					Type = src._Type,
					IndexA = src._PointA._Index,
					IndexB = src._PointB._Index,
					Length = src._Length,
					Shrink = _ShearShrink,
					Stretch = _ShearStretch,
					IsCollision = !src._PointA._IsFixed && !src._PointB._IsFixed && _IsCollideShear ? 1 : 0
				};
				PushConstraintTable(ConstraintTable, c);
			}
		}
		if (_IsComputeBendingVertical) {
			foreach (var src in _ConstraintsBendingVertical) {
				var c = new SPCRJointDynamicsJob.Constraint {
					Type = src._Type,
					IndexA = src._PointA._Index,
					IndexB = src._PointB._Index,
					Length = src._Length,
					Shrink = _BendingShrinkVertical,
					Stretch = _BendingStretchVertical,
					IsCollision = !src._PointA._IsFixed && !src._PointB._IsFixed && _IsCollideBendingVertical ? 1 : 0
				};
				PushConstraintTable(ConstraintTable, c);
			}
		}
		if (_IsComputeStructuralVertical) {
			foreach (var src in _ConstraintsStructuralVertical) {
				var c = new SPCRJointDynamicsJob.Constraint {
					Type = src._Type,
					IndexA = src._PointA._Index,
					IndexB = src._PointB._Index,
					Length = src._Length,
					Shrink = _StructuralShrinkVertical,
					Stretch = _StructuralStretchVertical,
					IsCollision = !src._PointA._IsFixed && !src._PointB._IsFixed && _IsCollideStructuralVertical ? 1 : 0
				};
				PushConstraintTable(ConstraintTable, c);
			}
		}

		_ConstraintTable = new SPCRJointDynamicsJob.Constraint[ConstraintTable.Count][];
		for (int i = 0; i < ConstraintTable.Count; ++i) {
			_ConstraintTable[i] = ConstraintTable[i].ToArray();
		}
	}

	public void OnDrawGizmos() {
		Gizmos.color = Color.magenta;
		_Job.DrawGizmos_Points();

		if (Application.isPlaying) {
			if (_IsDebugDraw_StructuralVertical) {
				Gizmos.color = Color.green;
				for (int i = 0; i < _ConstraintsStructuralVertical.Length; i++) {
					var constraint = _ConstraintsStructuralVertical[i];
					if (constraint == null)
						continue;
					var A = constraint._PointA;
					var B = constraint._PointB;
					if (A != null && B != null) {
						_Job.DrawGizmos_Constraints(A._Index, B._Index);
					}
				}
			}
			if (_IsDebugDraw_StructuralHorizontal) {
				Gizmos.color = Color.red;
				for (int i = 0; i < _ConstraintsStructuralHorizontal.Length; i++) {
					var constraint = _ConstraintsStructuralHorizontal[i];
					if (constraint == null)
						continue;
					var A = constraint._PointA;
					var B = constraint._PointB;
					if (A != null && B != null) {
						_Job.DrawGizmos_Constraints(A._Index, B._Index);
					}
				}
			}
		} else {
			if (_IsDebugDraw_StructuralVertical) {
				Gizmos.color = Color.green;
				for (int i = 0; i < _ConstraintsStructuralVertical.Length; i++) {
					var constraint = _ConstraintsStructuralVertical[i];
					if (constraint == null)
						continue;
					var A = constraint._PointA;
					var B = constraint._PointB;
					if (A != null && B != null) {
						var pointA = A.transform.position;
						var pointB = B.transform.position;
						Gizmos.DrawLine(pointA, pointB);
					}
				}
			}
			if (_IsDebugDraw_StructuralHorizontal) {
				Gizmos.color = Color.red;
				for (int i = 0; i < _ConstraintsStructuralHorizontal.Length; i++) {
					var constraint = _ConstraintsStructuralHorizontal[i];
					if (constraint == null)
						continue;
					var A = constraint._PointA;
					var B = constraint._PointB;
					if (A != null && B != null) {
						var pointA = A.transform.position;
						var pointB = B.transform.position;
						Gizmos.DrawLine(pointA, pointB);
					}
				}
			}
		}
	}
}
