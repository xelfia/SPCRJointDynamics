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

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public unsafe class SPCRJointDynamicsJob {
	public struct Point {
		public int Parent;
		public int Child;
		public float Weight;
		public float Mass;
		public float Resistance;
		public float FrictionScale;
		public float ParentLength;
		public float StructuralShrinkVertical;
		public float StructuralStretchVertical;
		public float StructuralShrinkHorizontal;
		public float StructuralStretchHorizontal;
		public float ShearShrink;
		public float ShearStretch;
		public float BendingShrinkVertical;
		public float BendingStretchVertical;
		public float BendingShrinkHorizontal;
		public float BendingStretchHorizontal;
		public Vector3 Gravity;
		public Vector3 BoneAxis;
		public Vector3 InitialPosition;
		public Quaternion LocalRotation;
		public Vector3 Position;
		public Vector3 OldPosition;
		public Vector3 PreviousDirection;
	}

	struct PointRead {
		public int Parent;
		public int Child;
		public float Weight;
		public float Mass;
		public float Resistance;
		public float FrictionScale;
		public float ParentLength;
		public float StructuralShrinkVertical;
		public float StructuralStretchVertical;
		public float StructuralShrinkHorizontal;
		public float StructuralStretchHorizontal;
		public float ShearShrink;
		public float ShearStretch;
		public float BendingShrinkVertical;
		public float BendingStretchVertical;
		public float BendingShrinkHorizontal;
		public float BendingStretchHorizontal;
		public Vector3 Gravity;
		public Vector3 BoneAxis;
		public Vector3 InitialPosition;
		public Quaternion LocalRotation;
	}

	struct PointReadWrite {
		public Vector3 Position;
		public Vector3 OldPosition;
		public Vector3 PreviousDirection;
		public float Friction;
	}

	public struct Constraint {
		public int IsCollision;
		public SPCRJointDynamicsController.ConstraintType Type;
		public int IndexA;
		public int IndexB;
		public float Length;
		public float Shrink;
		public float Stretch;
	}

	struct Collider {
		public float Radius;
		public float Height;
		public float Friction;
	}

	struct ColliderEx {
		public Vector3 Position;
		public Vector3 Direction;
	}

	Transform _RootBone;
	int _PointCount;
	NativeArray<PointRead> _PointsR;
	NativeArray<PointReadWrite> _PointsRW;
	NativeArray<Constraint>[] _Constraints;
	Transform[] _PointTransforms;
	TransformAccessArray _TransformArray;
	SPCRJointDynamicsCollider[] _RefColliders;
	NativeArray<Collider> _Colliders;
	NativeArray<ColliderEx> _ColliderExs;
	JobHandle _hJob = default(JobHandle);

	public void Initialize(Transform RootBone, Point[] Points, Transform[] PointTransforms, Constraint[][] Constraints, SPCRJointDynamicsCollider[] Colliders) {
		_RootBone = RootBone;
		_PointCount = Points.Length;

		var PointsR = new PointRead[_PointCount];
		var PointsRW = new PointReadWrite[_PointCount];
		for (int i = 0; i < Points.Length; ++i) {
			var src = Points[i];
			PointsR[i].Parent = src.Parent;
			PointsR[i].Child = src.Child;
			PointsR[i].Weight = src.Weight;
			PointsR[i].Mass = src.Mass;
			PointsR[i].Resistance = src.Resistance;
			PointsR[i].FrictionScale = src.FrictionScale;
			PointsR[i].ParentLength = src.ParentLength;
			PointsR[i].StructuralShrinkHorizontal = src.StructuralShrinkHorizontal * 0.5f;
			PointsR[i].StructuralStretchHorizontal = src.StructuralStretchHorizontal * 0.5f;
			PointsR[i].StructuralShrinkVertical = src.StructuralShrinkVertical * 0.5f;
			PointsR[i].StructuralStretchVertical = src.StructuralStretchVertical * 0.5f;
			PointsR[i].ShearShrink = src.ShearShrink * 0.5f;
			PointsR[i].ShearStretch = src.ShearStretch * 0.5f;
			PointsR[i].BendingShrinkHorizontal = src.BendingShrinkHorizontal * 0.5f;
			PointsR[i].BendingStretchHorizontal = src.BendingStretchHorizontal * 0.5f;
			PointsR[i].BendingShrinkVertical = src.BendingShrinkVertical * 0.5f;
			PointsR[i].BendingStretchVertical = src.BendingStretchVertical * 0.5f;
			PointsR[i].Gravity = src.Gravity;
			PointsR[i].BoneAxis = src.BoneAxis;
			PointsR[i].LocalRotation = src.LocalRotation;
			PointsR[i].InitialPosition = src.InitialPosition;
			PointsRW[i].Position = src.Position;
			PointsRW[i].OldPosition = src.OldPosition;
			PointsRW[i].PreviousDirection = src.PreviousDirection;
			PointsRW[i].Friction = 0.5f;
		}

		_PointsR = new NativeArray<PointRead>(_PointCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		_PointsR.CopyFrom(PointsR);
		_PointsRW = new NativeArray<PointReadWrite>(_PointCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		_PointsRW.CopyFrom(PointsRW);

		_PointTransforms = new Transform[_PointCount];
		for (int i = 0; i < _PointCount; ++i) {
			_PointTransforms[i] = PointTransforms[i];
		}

		_TransformArray = new TransformAccessArray(_PointTransforms);

		_Constraints = new NativeArray<Constraint>[Constraints.Length];
		for (int i = 0; i < Constraints.Length; ++i) {
			var src = Constraints[i];
			_Constraints[i] = new NativeArray<Constraint>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			_Constraints[i].CopyFrom(src);
		}

		_RefColliders = Colliders;
		var ColliderR = new Collider[Colliders.Length];
		for (int i = 0; i < Colliders.Length; ++i) {
			ColliderR[i].Radius = Colliders[i].Radius;
			ColliderR[i].Height = Colliders[i].Height;
			ColliderR[i].Friction = Colliders[i].Friction;
		}
		_Colliders = new NativeArray<Collider>(Colliders.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		_Colliders.CopyFrom(ColliderR);
		_ColliderExs = new NativeArray<ColliderEx>(Colliders.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

		_hJob = default(JobHandle);
	}

	public void Uninitialize() {
		WaitForComplete();
		_hJob = default(JobHandle);

		_ColliderExs.Dispose();
		_Colliders.Dispose();
		for (int i = 0; i < _Constraints.Length; ++i) {
			_Constraints[i].Dispose();
		}
		_TransformArray.Dispose();
		_PointsR.Dispose();
		_PointsRW.Dispose();
	}

	public void Reset() {
		var pPointRW = (PointReadWrite*)_PointsRW.GetUnsafePtr();
		for (int i = 0; i < _PointCount; ++i) {
			pPointRW[i].OldPosition = pPointRW[i].Position = _PointTransforms[i].position;
		}
	}

	public void Restore() {
		var pPointR = (PointRead*)_PointsR.GetUnsafePtr();
		var pPointRW = (PointReadWrite*)_PointsRW.GetUnsafePtr();
		for (int i = 0; i < _PointCount; ++i) {
			pPointRW[i].Position = _RootBone.TransformPoint(pPointR[i].InitialPosition);
			pPointRW[i].OldPosition = pPointRW[i].Position;
			_PointTransforms[i].position = pPointRW[i].Position;
		}
	}

	public void Execute(
		float StepTime, Vector3 WindForce,
		int Relaxation, float SpringK,
		bool IsEnableFloorCollision, float FloorHeight,
		bool IsEnableColliderCollision) {
		WaitForComplete();

		var pRPoints = (PointRead*)_PointsR.GetUnsafePtr();
		var pRWPoints = (PointReadWrite*)_PointsRW.GetUnsafePtr();
		var pColliders = (Collider*)_Colliders.GetUnsafePtr();
		var pColliderExs = (ColliderEx*)_ColliderExs.GetUnsafePtr();

		var ColliderCount = _RefColliders.Length;
		for (int i = 0; i < ColliderCount; ++i) {
			var pDst = pColliderExs + i;
			var Src = _RefColliders[i];
			var SrcT = Src.RefTransform;
			if (Src.Height <= 0.0f) {
				pDst->Position = _RefColliders[i].RefTransform.position;
			} else {
				pDst->Direction = SrcT.rotation * Vector3.up * Src.Height;
				pDst->Position = SrcT.position - (pDst->Direction * 0.5f);
			}
		}

		var PointUpdate = new JobPointUpdate();
		PointUpdate.pRPoints = pRPoints;
		PointUpdate.pRWPoints = pRWPoints;
		PointUpdate.WindForce = WindForce;
		PointUpdate.StepTime_x2_Half = StepTime * StepTime * 0.5f;
		_hJob = PointUpdate.Schedule(_PointCount, 8);

		for (int i = 0; i < Relaxation; ++i) {
			foreach (var constraint in _Constraints) {
				var ConstraintUpdate = new JobConstraintUpdate();
				ConstraintUpdate.pConstraints = (Constraint*)constraint.GetUnsafePtr();
				ConstraintUpdate.pRPoints = pRPoints;
				ConstraintUpdate.pRWPoints = pRWPoints;
				ConstraintUpdate.pColliders = pColliders;
				ConstraintUpdate.pColliderExs = pColliderExs;
				ConstraintUpdate.ColliderCount = ColliderCount;
				ConstraintUpdate.SpringK = SpringK;
				_hJob = ConstraintUpdate.Schedule(constraint.Length, 8, _hJob);
			}
		}

		if (IsEnableFloorCollision || IsEnableColliderCollision) {
			var CollisionPoint = new JobCollisionPoint();
			CollisionPoint.pRWPoints = pRWPoints;
			CollisionPoint.pColliders = pColliders;
			CollisionPoint.pColliderExs = pColliderExs;
			CollisionPoint.ColliderCount = ColliderCount;
			CollisionPoint.FloorHeight = FloorHeight;
			CollisionPoint.IsEnableFloor = IsEnableFloorCollision;
			CollisionPoint.IsEnableCollider = IsEnableColliderCollision;
			_hJob = CollisionPoint.Schedule(_PointCount, 8, _hJob);
		}

		var PointToTransform = new JobPointToTransform();
		PointToTransform.pRPoints = pRPoints;
		PointToTransform.pRWPoints = pRWPoints;
		_hJob = PointToTransform.Schedule(_TransformArray, _hJob);
	}

	public void WaitForComplete() {
		_hJob.Complete();
		_hJob = default(JobHandle);
	}

	public void DrawGizmos_Points() {
		Gizmos.color = Color.blue;
		for (int i = 0; i < _PointCount; ++i) {
			Gizmos.DrawSphere(_PointsRW[i].Position, 0.005f);
		}
	}

	public void DrawGizmos_Constraints(int A, int B) {
		if (_PointTransforms == null ||
			A <= 0 || B <= 0 || A >= _PointTransforms.Length || B >= _PointTransforms.Length)
			return;
		Gizmos.DrawLine(_PointTransforms[A].position, _PointTransforms[B].position);
	}

	[BurstCompile]
	struct JobPointUpdate : IJobParallelFor {
		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public PointRead* pRPoints;
		[NativeDisableUnsafePtrRestriction]
		public PointReadWrite* pRWPoints;

		[ReadOnly]
		public Vector3 WindForce;
		[ReadOnly]
		public float StepTime_x2_Half;

		void IJobParallelFor.Execute(int index) {
			var pR = pRPoints + index;
			if (pR->Weight == 0.0f)
				return;

			var pRW = pRWPoints + index;

			Vector3 Force = Vector3.zero;
			Force += pR->Gravity;
			Force += WindForce;
			Force *= StepTime_x2_Half;

			Vector3 Displacement;
			Displacement = pRW->Position - pRW->OldPosition;
			Displacement += Force / pR->Mass;
			Displacement *= pR->Resistance;
			Displacement *= 1.0f - (pRW->Friction * pR->FrictionScale);

			pRW->OldPosition = pRW->Position;
			pRW->Position += Displacement;
			pRW->Friction = 0.0f;
		}
	}

	[BurstCompile]
	struct JobConstraintUpdate : IJobParallelFor {
		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public Constraint* pConstraints;

		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public PointRead* pRPoints;
		[NativeDisableUnsafePtrRestriction]
		public PointReadWrite* pRWPoints;

		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public Collider* pColliders;
		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public ColliderEx* pColliderExs;
		[ReadOnly]
		public int ColliderCount;

		[ReadOnly]
		public float SpringK;

		void IJobParallelFor.Execute(int index) {
			var constraint = pConstraints + index;
			var RptA = pRPoints + constraint->IndexA;
			var RptB = pRPoints + constraint->IndexB;

			float WeightA = RptA->Weight;
			float WeightB = RptB->Weight;

			if ((WeightA == 0.0) && (WeightB == 0.0))
				return;

			var RWptA = pRWPoints + constraint->IndexA;
			var RWptB = pRWPoints + constraint->IndexB;

			var Direction = RWptB->Position - RWptA->Position;

			float Distance = Direction.magnitude;
			float Force = (Distance - constraint->Length) * SpringK;

			bool IsShrink = Force >= 0.0f;
			float ConstraintPower;
			switch (constraint->Type) {
				case SPCRJointDynamicsController.ConstraintType.Structural_Vertical:
					ConstraintPower = IsShrink
						? constraint->Shrink * (RptA->StructuralShrinkVertical + RptB->StructuralShrinkVertical)
						: constraint->Stretch * (RptA->StructuralStretchVertical + RptB->StructuralStretchVertical);
					break;
				case SPCRJointDynamicsController.ConstraintType.Structural_Horizontal:
					ConstraintPower = IsShrink
						? constraint->Shrink * (RptA->StructuralShrinkHorizontal + RptB->StructuralShrinkHorizontal)
						: constraint->Stretch * (RptA->StructuralStretchHorizontal + RptB->StructuralStretchHorizontal);
					break;
				case SPCRJointDynamicsController.ConstraintType.Shear:
					ConstraintPower = IsShrink
						? constraint->Shrink * (RptA->ShearShrink + RptB->ShearShrink)
						: constraint->Stretch * (RptA->ShearStretch + RptB->ShearStretch);
					break;
				case SPCRJointDynamicsController.ConstraintType.Bending_Vertical:
					ConstraintPower = IsShrink
						? constraint->Shrink * (RptA->BendingShrinkVertical + RptB->BendingShrinkVertical)
						: constraint->Stretch * (RptA->BendingStretchVertical + RptB->BendingStretchVertical);
					break;
				case SPCRJointDynamicsController.ConstraintType.Bending_Horizontal:
					ConstraintPower = IsShrink
						? constraint->Shrink * (RptA->BendingShrinkHorizontal + RptB->BendingShrinkHorizontal)
						: constraint->Stretch * (RptA->BendingStretchHorizontal + RptB->BendingStretchHorizontal);
					break;
				default:
					ConstraintPower = 0.0f;
					break;
			}

			if (ConstraintPower > 0.0f) {
				var Displacement = Direction.normalized * (Force * ConstraintPower);

				float WightAB = WeightA + WeightB;
				RWptA->Position += Displacement * WeightA / WightAB;
				RWptB->Position -= Displacement * WeightB / WightAB;
			}

			if (constraint->IsCollision == 0)
				return;

			var Friction = 0.0f;
			for (int i = 0; i < ColliderCount; ++i) {
				Collider* pCollider = pColliders + i;
				ColliderEx* pColliderEx = pColliderExs + i;

				if (CollisionDetection(pCollider, pColliderEx, RWptA->Position, RWptB->Position, out Vector3 pointOnLine, out Vector3 pointOnCollider)) {
					var Pushout = pointOnLine - pointOnCollider;
					var PushoutDistance = Pushout.magnitude;

					var pointDistance = (RWptB->Position - RWptA->Position).magnitude * 0.5f;
					var rateP1 = Mathf.Clamp01((pointOnLine - RWptA->Position).magnitude / pointDistance);
					var rateP2 = Mathf.Clamp01((pointOnLine - RWptB->Position).magnitude / pointDistance);

					Pushout /= PushoutDistance;
					Pushout *= Mathf.Max(pCollider->Radius - PushoutDistance, 0.0f);
					RWptA->Position += Pushout * rateP2;
					RWptB->Position += Pushout * rateP1;

					var Dot = Vector3.Dot(Vector3.up, (pointOnLine - pointOnCollider).normalized);
					Friction = Mathf.Max(Friction, pCollider->Friction * Mathf.Clamp01(Dot));
				}
			}

			RWptA->Friction = Mathf.Max(Friction, RWptA->Friction);
			RWptB->Friction = Mathf.Max(Friction, RWptB->Friction);
		}

		bool CollisionDetection(Collider* pCollider, ColliderEx* pColliderEx, Vector3 point1, Vector3 point2, out Vector3 pointOnLine, out Vector3 pointOnCollider) {
			if (pCollider->Height <= 0.0f) {
				var direction = point2 - point1;
				var directionLength = direction.magnitude;
				direction /= directionLength;

				var toCenter = pColliderEx->Position - point1;
				var dot = Vector3.Dot(direction, toCenter);
				var pointOnDirection = direction * Mathf.Clamp(dot, 0.0f, directionLength);

				pointOnCollider = pColliderEx->Position;
				pointOnLine = pointOnDirection + point1;

				if ((pointOnCollider - pointOnLine).sqrMagnitude > pCollider->Radius * pCollider->Radius) {
					return false;
				}

				return true;
			} else {
				var capsuleDir = pColliderEx->Direction;
				var capsulePos = pColliderEx->Position;
				var pointDir = point2 - point1;

				float t1, t2;
				var sqrDistance = ComputeNearestPoints(capsulePos, capsuleDir, point1, pointDir, out t1, out t2, out pointOnCollider, out pointOnLine);
				if (sqrDistance > pCollider->Radius * pCollider->Radius) {
					pointOnCollider = Vector3.zero;
					pointOnLine = Vector3.zero;
					return false;
				}

				t1 = Mathf.Clamp01(t1);
				t2 = Mathf.Clamp01(t2);

				pointOnCollider = capsulePos + capsuleDir * t1;
				pointOnLine = point1 + pointDir * t2;

				return (pointOnCollider - pointOnLine).sqrMagnitude <= pCollider->Radius * pCollider->Radius;
			}
		}

		float ComputeNearestPoints(Vector3 posP, Vector3 dirP, Vector3 posQ, Vector3 dirQ, out float tP, out float tQ, out Vector3 pointOnP, out Vector3 pointOnQ) {
			var n1 = Vector3.Cross(dirP, Vector3.Cross(dirQ, dirP));
			var n2 = Vector3.Cross(dirQ, Vector3.Cross(dirP, dirQ));

			tP = Vector3.Dot((posQ - posP), n2) / Vector3.Dot(dirP, n2);
			tQ = Vector3.Dot((posP - posQ), n1) / Vector3.Dot(dirQ, n1);
			pointOnP = posP + dirP * tP;
			pointOnQ = posQ + dirQ * tQ;

			return (pointOnQ - pointOnP).sqrMagnitude;
		}
	}

	[BurstCompile]
	struct JobCollisionPoint : IJobParallelFor {
		[NativeDisableUnsafePtrRestriction]
		public PointReadWrite* pRWPoints;

		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public Collider* pColliders;
		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public ColliderEx* pColliderExs;
		[ReadOnly]
		public int ColliderCount;

		[ReadOnly]
		public float FloorHeight;

		[ReadOnly]
		public bool IsEnableFloor;
		public bool IsEnableCollider;

		void IJobParallelFor.Execute(int index) {
			var pRW = pRWPoints + index;

			if (IsEnableFloor) {
				if (pRW->Position.y <= FloorHeight) {
					pRW->Position.y = FloorHeight;
				}
			}

			if (IsEnableCollider) {
				for (int i = 0; i < ColliderCount; ++i) {
					Collider* pCollider = pColliders + i;
					ColliderEx* pColliderEx = pColliderExs + i;

					if (pCollider->Height <= 0.0f) {
						PushoutFromSphere(pCollider, pColliderEx, ref pRW->Position);
					} else {
						PushoutFromCapsule(pCollider, pColliderEx, ref pRW->Position);
					}
				}
			}
		}

		void PushoutFromSphere(Vector3 Center, float Radius, ref Vector3 point) {
			var direction = point - Center;
			var sqrDirectionLength = direction.magnitude;
			var radius = Radius;
			if (sqrDirectionLength > 0.001f) {
				if (sqrDirectionLength < radius * radius) {
					var directionLength = Mathf.Sqrt(sqrDirectionLength);
					var diff = radius - directionLength;
					point = direction * diff / directionLength;
					return;
				}
			}
		}

		void PushoutFromSphere(Collider* pCollider, ColliderEx* pColliderEx, ref Vector3 point) {
			PushoutFromSphere(pColliderEx->Position, pCollider->Radius, ref point);
		}

		void PushoutFromCapsule(Collider* pCollider, ColliderEx* pColliderEx, ref Vector3 point) {
			var capsuleVec = pColliderEx->Direction;
			var capsulePos = pColliderEx->Position;
			var targetVec = point - capsulePos;
			var distanceOnVec = Vector3.Dot(capsuleVec, targetVec);
			if (distanceOnVec <= 0.0f) {
				PushoutFromSphere(capsulePos, pCollider->Radius, ref point);
				return;
			} else if (distanceOnVec >= pCollider->Height) {
				PushoutFromSphere(capsulePos + capsuleVec * distanceOnVec, pCollider->Radius, ref point);
				return;
			} else {
				var positionOnVec = capsulePos + (capsuleVec * distanceOnVec);
				var pushoutVec = point - positionOnVec;
				var sqrPushoutDistance = pushoutVec.sqrMagnitude;
				if (sqrPushoutDistance > 0.001f) {
					if (sqrPushoutDistance < pCollider->Radius * pCollider->Radius) {
						var pushoutDistance = Mathf.Sqrt(sqrPushoutDistance);
						point = positionOnVec + pushoutVec * pCollider->Radius / pushoutDistance;
						return;
					}
				}
			}
		}
	}

	[BurstCompile]
	struct JobPointToTransform : IJobParallelForTransform {
		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public PointRead* pRPoints;
		[NativeDisableUnsafePtrRestriction]
		public PointReadWrite* pRWPoints;

		void IJobParallelForTransform.Execute(int index, TransformAccess transform) {
			var pRW = pRWPoints + index;
			var pR = pRPoints + index;

			if (pR->Weight > 0.0f) {
				var pRWP = pRWPoints + pR->Parent;
				var Direction = pRW->Position - pRWP->Position;
				var RealLength = Direction.magnitude;
				if (RealLength > 0.001f) {
					pRW->PreviousDirection = Direction;
					transform.position = pRW->Position;
					SetRotation(index, transform);
				} else {
					pRW->Position = pRWP->Position + pRW->PreviousDirection;
				}
			} else {
				pRW->Position = transform.position;
				SetRotation(index, transform);
			}
		}

		void SetRotation(int index, TransformAccess transform) {
			var pR = pRPoints + index;
			var pRW = pRWPoints + index;

			transform.localRotation = Quaternion.identity * pR->LocalRotation;
			if (pR->Child != -1) {
				var pRWC = pRWPoints + pR->Child;
				var Direction = pRWC->Position - pRW->Position;
				if (Direction.magnitude > 0.001f) {
					Matrix4x4 mRotate = Matrix4x4.Rotate(transform.rotation);
					Vector3 AimVector = mRotate * pR->BoneAxis;
					Quaternion AimRotation = Quaternion.FromToRotation(AimVector, Direction);
					transform.rotation = AimRotation * transform.rotation;
				}
			}
		}
	}
}
