using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyCone;

public static class TrainCarHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static Vector3? GetApproxStandardConePosition(TrainCar trainCar, bool isRear = false)
    {
        var coupler = isRear ? trainCar.rearCoupler : trainCar.frontCoupler;
        var collider = coupler.GetComponent<BoxCollider>();

        Bounds b = collider.bounds;

        Vector3 c = b.center;
        Vector3 e = b.extents;

        Vector3 dir = isRear ? -coupler.transform.forward : coupler.transform.forward;

        float distance =
            Mathf.Abs(Vector3.Dot(dir, Vector3.right)) * b.extents.x +
            Mathf.Abs(Vector3.Dot(dir, Vector3.up)) * b.extents.y +
            Mathf.Abs(Vector3.Dot(dir, Vector3.forward)) * b.extents.z;

        Vector3 bestPos = b.center + dir.normalized * (isRear ? -distance : distance);

        bestPos.y -= 0.75f;

        bestPos = trainCar.transform.InverseTransformPoint(bestPos);

        return bestPos;
    }

    public static Vector3? GetApproxStandardRearConePosition(TrainCar trainCar)
    {
        return GetApproxStandardConePosition(trainCar, isRear: true);
    }

    public static Vector3? GetApproxStandardFrontConePosition(TrainCar trainCar)
    {
        return GetApproxStandardConePosition(trainCar, isRear: false);
    }
}