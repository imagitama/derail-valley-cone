using DerailValleyBindingHelper;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyCone;

public enum StandardSide
{
    Front,
    Rear,
}

public class ConeComponent : MonoBehaviour
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    public ConeSettings settings;
    public StandardSide? side;
    public TrainCar trainCar;
    private Rigidbody rb;


    void Start()
    {
        Logger.Log($"Cone.Start car={trainCar} side={side}");

        var collider = GetComponent<MeshCollider>();

        if (collider == null)
            throw new System.Exception("Need a mesh collider");

        collider.convex = true;
        collider.isTrigger = true;

        // if (rb == null)
        //     rb = gameObject.AddComponent<Rigidbody>();

        // rb.useGravity = false;
        // rb.isKinematic = true;
        // rb.interpolation = RigidbodyInterpolation.None;
        // rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }

    private bool _isVisible = true;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
                return;

            Logger.Log($"Cone.IsVisible {_isVisible} => {value}");

            _isVisible = value;

            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.enabled = _isVisible;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Logger.Log($"TRIGGER col={other}");

        // TODO: more reliable way
        var isBogie = other.gameObject.name == "[coupler front]" || other.gameObject.name == "[coupler rear]";
        if (!isBogie)
            return;

        TrainCar? collidingCar = other.transform.parent.GetComponent<TrainCar>();

        // Logger.Log($"CAR car={collidingCar}");

        // Logger.Log($"Bogie={isBogie} Parent={parent} car={car}");

        if (collidingCar == null || collidingCar == trainCar || collidingCar.derailed)
            return;

        Logger.Log($"Derail train: {collidingCar}");

        collidingCar.Derail();
        AddForce(collidingCar.rb, other.transform);

        if (Main.settings.DerailTrainset)
        {
            Logger.Log($"Derail trainset: {collidingCar.trainset.cars.Count} cars");

            foreach (var carInTrainset in collidingCar.trainset.cars)
            {
                if (carInTrainset != collidingCar)
                {
                    carInTrainset.Derail();

                    // TODO: determine bogie based on connection with first car
                    AddForce(carInTrainset.rb, carInTrainset.FrontBogie.transform);
                }
            }
        }
    }

    void AddForce(Rigidbody rigidbodyToAddTo, Transform transform)
    {
        float impulse = Main.settings.TossAwayForce;

        if (impulse == 0f)
            return;

        float side = Random.value < 0.5f ? -1f : 1f;
        float angle = Random.Range(30f, 60f) * side;

        Quaternion tilt = Quaternion.AngleAxis(angle, transform.forward);
        Vector3 forceDir = tilt * Vector3.up;
        Vector3 force = forceDir.normalized * impulse;

        rigidbodyToAddTo.AddForce(force, ForceMode.Impulse);

        Logger.Log($"Add force={force} angle={angle} impulse={impulse}");
    }

    // void OnCollisionEnter(Collision collision)
    // {
    //     var impulseMagnitude = collision.impulse.magnitude;

    //     var collidingCar = GetCarFromCollision(collision);

    //     // Logger.Log($"COLLIDE car={car} impulse={impulseMagnitude} threshold={Main.settings.DerailThreshold}");

    //     if (collidingCar != null && collidingCar != trainCar)
    //     {
    //         Logger.Log($"ConeComponent.Impact car={collidingCar} impulse={impulseMagnitude} threshold={Main.settings.DerailThreshold}");

    //         if (Main.settings.DerailThreshold > 0 && impulseMagnitude >= Main.settings.DerailThreshold)
    //         {
    //             var point = collision.GetContact(0).point;

    //             if (!collidingCar.derailed)
    //             {
    //                 Logger.Log($"Derail train: {collidingCar}");

    //                 // void OnDerailed(TrainCar car)
    //                 // {
    //                 //     Logger.Log($"Train derailed: {collidingCar}");

    //                 //     ActivatePhysics(collidingCar.rb, collision.impulse, point);

    //                 //     collidingCar.OnDerailed -= OnDerailed;
    //                 // }

    //                 // collidingCar.OnDerailed += OnDerailed;

    //                 collidingCar.Derail();


    //                 ActivatePhysics(collidingCar.rb, collision.impulse, point);
    //             }
    //             else
    //             {
    //                 // ActivatePhysics(collidingCar.rb, collision.impulse, point);
    //             }
    //         }
    //     }
    // }

    // void ActivatePhysics(Rigidbody collidingRb, Vector3 impulse, Vector3 point)
    // {
    //     // rb.isKinematic = false;
    //     // rb.constraints = RigidbodyConstraints.None;

    //     // rb.velocity = Vector3.zero;
    //     // rb.angularVelocity = Vector3.zero;

    //     Logger.Log($"Add force impulse={impulse} point={point}");

    //     var force = Vector3.up * 100000;

    //     collidingRb.AddForceAtPosition(force, point, ForceMode.Impulse);

    //     collidingRb.AddForceAtPosition(impulse, point, ForceMode.Impulse);

    //     // var velocity = impulse / collidingRb.mass;
    //     // collidingRb.velocity = velocity;
    // }

    // TrainCar? GetCarFromCollision(Collision collision)
    // {
    //     var current = collision.collider.transform;

    //     while (current != null)
    //     {
    //         var car = current.GetComponent<TrainCar>();

    //         if (car != null)
    //             return car;

    //         current = current.parent;
    //     }

    //     return null;
    // }
}