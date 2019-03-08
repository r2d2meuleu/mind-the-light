﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Spy : Actor {

   [SerializeField]
   private float maxHealth = 100f;
   private float curHealth;
   private bool isFlashing;

   public AudioClip takeDmgSound;
   public AudioClip deathSound;

   private Coroutine respawnCoroutine;

   private new void Awake() {
      base.Awake();
   }

   public void OnSpyHit(Actor damager, float damage) {
      if (isDead)
         return;

      if (PhotonNetwork.IsMasterClient)
         p.PV.RPC("RPC_OnSpyHit", RpcTarget.All, damager.p.NickName, damage);
   }

   private void Die(string killerName) {
      isDead = true;
      // Disable components
      Debug.Log(transform.name + " is dead.");
      Server.Death(killerName, p.NickName);

      audioS.PlayOneShot(deathSound);
      anim.SetBool("Dead", true);

      respawnCoroutine = StartCoroutine(Respawn());
   }

   private IEnumerator Respawn() {
      yield return new WaitForSeconds(3f);

      SetDefaults();
      int rand = Random.Range(0, GameManager.Instance.spawnPointsSpies.Length);
      Teleport(GameManager.Instance.spawnPointsSpies[rand].position);
      //Transform spawnPoint = NetworkManager.singleton.GetStartPosition();
      //transform.position = spawnPoint.position;
      //transform.rotation = spawnPoint.rotation;
      //Debug.Log(transform.name + " respawned.");
   }

   private IEnumerator HitFlashAnim() {
      isFlashing = true;

      sr.material.SetFloat("_FlashAmount", 0.9f);
      yield return new WaitForSeconds(0.15f);
      sr.material.SetFloat("_FlashAmount", 0f);
      yield return new WaitForSeconds(0.5f);
      
      isFlashing = false;
   }

   public override void SetDefaults() {
      if(PhotonNetwork.IsMasterClient)
         p.PV.RPC("RPC_SetDefaults", RpcTarget.All);
   }

   [PunRPC]
   public override void RPC_SetDefaults() {
      base.RPC_SetDefaults();

      Debug.Log("RPC_SetDefaults");

      if (respawnCoroutine != null) {
         StopCoroutine(respawnCoroutine);
         respawnCoroutine = null;
      }

      isDead = false;
      curHealth = maxHealth;
      anim.SetBool("Dead", false);
      HUD.Instance.UpdateHealthBar(curHealth / maxHealth);
   }

   [PunRPC]
   private void RPC_OnSpyHit(string damagerName, float damage) {
      if (isDead)
         return;

      curHealth -= damage;
      HUD.Instance.UpdateHealthBar(curHealth / maxHealth);
      Debug.Log(transform.name + " now has " + curHealth + "hp.");

      if (curHealth <= 0) {
         Die(damagerName);
      }
      else {
         audioS.PlayOneShot(takeDmgSound);
         if (!isFlashing) {
            StartCoroutine(HitFlashAnim());
         }
      }
   }
}
