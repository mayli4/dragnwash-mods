using BepInEx;
using GoldMeridian.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using Diagnostics = System.Diagnostics;

namespace Freeplay;

[ExtensionDataFor<PlayerController>("Noclip")]
internal class NoclipData {
    public bool IsFlying { get; set; }
    public float FlySpeed { get; set; } = 10f;
}

[BepInPlugin("com.meshlet.noclip", "DragnWash Noclip", "1.0.0"), UsedImplicitly]
internal sealed class Plugin : BaseUnityPlugin {
    
    [UsedImplicitly]
    private void Awake() {
        On.PlayerController.Update += (orig, self) => {
            var noclip = self.Noclip ??= new NoclipData();

            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) {
                noclip.IsFlying = !noclip.IsFlying;

                var charController = self.GetComponent<DragonCharacterController>();
                var rb = self.GetComponent<Rigidbody>();
                
                if (charController != null) {
                    charController.enabled = !noclip.IsFlying;
                }
                
                Diagnostics.Debug.Assert(rb != null);
                
                rb.isKinematic = noclip.IsFlying;
                rb.useGravity = !noclip.IsFlying;
            }
            
            if (noclip.IsFlying) {
                HandleFlightControls(self, noclip);
            }
            orig(self);
        };
        
        Debug.Log("[com.meshlet.noclip] plugin successfully loaded!");
    }
    
    private static void HandleFlightControls(PlayerController player, NoclipData noclip) {
        if (Keyboard.current == null) return;

        if (Mouse.current != null) {
            float scroll = Mouse.current.scroll.ReadValue().y;
            noclip.FlySpeed = scroll switch
            {
                > 0f => Mathf.Min(noclip.FlySpeed + 2f, 50f),
                < 0f => Mathf.Max(noclip.FlySpeed - 2f, 2f),
                _ => noclip.FlySpeed
            };
        }

        var moveDirection = Vector3.zero;
        var cameraTransform = Camera.main != null ? Camera.main.transform : player.transform;

        if (Keyboard.current.wKey.isPressed) moveDirection += cameraTransform.forward;
        if (Keyboard.current.sKey.isPressed) moveDirection -= cameraTransform.forward;
        if (Keyboard.current.aKey.isPressed) moveDirection -= cameraTransform.right;
        if (Keyboard.current.dKey.isPressed) moveDirection += cameraTransform.right;

        if (Keyboard.current.spaceKey.isPressed) moveDirection += Vector3.up;
        if (Keyboard.current.leftShiftKey.isPressed) moveDirection += Vector3.down;

        player.transform.position += moveDirection.normalized * (noclip.FlySpeed * Time.deltaTime);

        var lookController = player.GetComponent<LookController>();
        
        if(lookController == null || Mouse.current == null)
            return;
        
        var lookInput = Mouse.current.delta.ReadValue() * 0.1f;
        lookController.LookInput = lookInput;
    }
}