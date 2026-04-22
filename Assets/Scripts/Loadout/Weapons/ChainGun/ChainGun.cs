using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChainGun : Weapon
{
    [SerializeField] private Transform barrelTransform;
    [SerializeField] private AudioSource barrelAudioSource;
    [SerializeField] private AudioClip spinupSound;
    [SerializeField] private AudioClip spinLoopSound;
    [SerializeField] private AudioClip spindownSound;
    [SerializeField] private ParticleSystem muzzleFlashParticleSystem;

    private float barrelSpinMaxSpeed = 600f;
    private float barrelSpeedRatio = 0f;
    private bool isBarrelSpinning = false;

    private float currentBarrelSpinSpeed = 0f;

        public sealed override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
    
            barrelAudioSource.spatialBlend = 1;
            barrelAudioSource.dopplerLevel = 0;
            barrelAudioSource.minDistance = 1;
            barrelAudioSource.maxDistance = 10;
        }

    protected sealed override void Update()
    {
        base.Update();

        if (!isInitialized) return;
        if (!isEquiped.Value)
        {
            RotateBarrelRpc(0f);
            return;
        }
        if (IsServer)
        {
            if (isTryingToFire.Value) barrelSpeedRatio = Mathf.Lerp(barrelSpeedRatio, 1f, Time.deltaTime * 2f);
            else barrelSpeedRatio = Mathf.Lerp(barrelSpeedRatio, 0f, Time.deltaTime * 2f);

            barrelSpeedRatio = barrelSpeedRatio > 0.01f ? barrelSpeedRatio < 0.99f ? barrelSpeedRatio : 1f : 0f;
            if (currentBarrelSpinSpeed != barrelSpinMaxSpeed * barrelSpeedRatio) RotateBarrelRpc(barrelSpinMaxSpeed * barrelSpeedRatio);
        }

        barrelTransform.Rotate(Vector3.up, currentBarrelSpinSpeed * Time.deltaTime);
        // Start Spin
        if (!isBarrelSpinning && isTryingToFire.Value)
        {
            barrelAudioSource.clip = spinupSound;
            barrelAudioSource.loop = false;
            barrelAudioSource.Play();
        }
        // Loop Spin
        else if (barrelAudioSource.clip == spinupSound && !barrelAudioSource.isPlaying)
        {
            barrelAudioSource.clip = spinLoopSound;
            barrelAudioSource.loop = true;
            barrelAudioSource.Play();
        }
        else if (barrelAudioSource.clip == spinLoopSound && !isTryingToFire.Value)
        {
            barrelAudioSource.clip = spindownSound;
            barrelAudioSource.loop = false;
            barrelAudioSource.Play();
        }
        isBarrelSpinning = isTryingToFire.Value;
    }

    [Rpc(SendTo.Everyone)]
    private void RotateBarrelRpc(float speed) => currentBarrelSpinSpeed = speed;

    protected sealed override void PostFiredRpc()
    {
        base.PostFiredRpc();
        muzzleFlashParticleSystem.Emit(5);
    }
}
