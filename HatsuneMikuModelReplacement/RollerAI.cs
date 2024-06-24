using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace RollerEnemy
{
    class RollerAI : EnemyAI
    {

        public enum RollerState
        {
            CLOSED,
            SEARCHING,
            ARMING,
            ROLLING
        }
        const float speed = 10f;
        const float pupilSpeed = 0.08f;
        Animator animator;
        Material material;

        bool agressive = false;
        bool hitWall = false;        
        float startSeeingPlayer = 0;
        RollerState rollerState = RollerState.CLOSED;              
        float timeSinceHittingPlayer = 0;
        float stateTimeLeft = 0;

        Vector3 direction = Vector3.zero;        

        Transform targetedPlayer = null;
        public Transform secondEye=null;
        bool usingFirstEye = false;

        AudioSource Scream, Arm, Roll, WallHit,Open,Close;

        public override void Start()
        {            
            // EnemyAI attributes
            AIIntervalTime = 0.2f;
            updatePositionThreshold = 0.4f;
            moveTowardsDestination = false;
            syncMovementSpeed = 0.22f;
            exitVentAnimationTime = 1.45f;
            enemyHP = 1;

            base.Start();
            animator = GetComponent<Animator>();
            material = transform.Find("EyeModel").GetComponent<SkinnedMeshRenderer>().materials[0];
            material.SetFloat("_EyeOpeness",1f);

            Scream = transform.Find("AudioSources").Find("Scream").GetComponent<AudioSource>();
            Arm = transform.Find("AudioSources").Find("Arm").GetComponent<AudioSource>();
            Roll = transform.Find("AudioSources").Find("Roll").GetComponent<AudioSource>();
            WallHit = transform.Find("AudioSources").Find("WallHit").GetComponent<AudioSource>();
            Open = transform.Find("AudioSources").Find("Open").GetComponent<AudioSource>();
            Close = transform.Find("AudioSources").Find("Close").GetComponent<AudioSource>();

            //set radar layer correct (sometimes it is loaded incorrectly, why?)
            transform.Find("Circle").gameObject.layer = 1 << 14; //MapRadar layer
        }

        public override void Update()
        {            
                        
            base.Update();

            if (isEnemyDead) return;


            timeSinceHittingPlayer -= Time.deltaTime;
            stateTimeLeft -= Time.deltaTime;

            CheckState();                                        
        }

        private void LateUpdate()
        {
            if (targetedPlayer != null)
            {
                FocusEyeOnPlayer(targetedPlayer);
            }
        }

        private void CheckState()
        {
            switch (rollerState)
            {
                case RollerState.CLOSED:
                    if (stateTimeLeft <= 0)
                    {
                        SetStateServerRPC(RollerState.SEARCHING);
                    }                    
                    break;
                case RollerState.SEARCHING:                    
                    if (stateTimeLeft <= 0)
                    {
                        SetStateServerRPC(RollerState.CLOSED);
                        Close.Play();
                        agressive = false;
                    }
                    else
                    {
                        if (targetedPlayer == null)
                        {                            
                            material.SetFloat("_EyeOpeness", Mathf.Lerp(material.GetFloat("_EyeOpeness"), 1f, pupilSpeed));
                            PlayerControllerB player = PlayerVisible();
                            if (player != null)
                            {                                
                                targetedPlayer = player.transform;
                                startSeeingPlayer = Time.time;                                
                                player.JumpToFearLevel(1f);                                
                                Scream.Play();
                            }
                        }
                        else
                        {                            
                            if(Time.time > startSeeingPlayer + 0.5f)
                            {
                                SetStateClientRPC(RollerState.ARMING);
                            }                            
                        }
                        
                    }
                    break;
                case RollerState.ARMING:
                    if (stateTimeLeft <= 0)
                    {
                        SetStateClientRPC(RollerState.ROLLING);
                        agressive = true;
                    }
                    else
                    {
                        direction = (targetedPlayer.transform.position - transform.position);
                        direction.y = 0;
                        direction.Normalize();
                        agressive = true;
                        transform.LookAt(new Vector3(
                            targetedPlayer.transform.position.x,
                            transform.position.y,
                            targetedPlayer.transform.position.z
                            ));
                    }
                    break;
                case RollerState.ROLLING:
                    if (stateTimeLeft <= 0 && hitWall )
                    {
                        WallHit.Play();
                        Roll.Stop();
                        SetStateClientRPC(RollerState.CLOSED);
                    }                    
                    this.agent.Move(direction* speed * Time.deltaTime);

                    break;
            }
        }

            [ServerRpc]
        private void SetStateServerRPC(RollerState state)
        {
            SetStateClientRPC(state);
        }

            [ClientRpc]
        private void SetStateClientRPC(RollerState state)
        {            
            rollerState = state;            
            switch (state)
            {
                case RollerState.CLOSED:
                    stateTimeLeft = 3f;
                    targetedPlayer = null;
                    hitWall = false;
                    animator.SetBool("Closed",true);
                    animator.SetBool("Rolling",false);
                    animator.SetBool("Searching",false);                    
                    break;
                case RollerState.SEARCHING:
                    stateTimeLeft = 4f;
                    animator.SetBool("Closed", false);
                    animator.SetBool("Rolling", false);
                    animator.SetBool("Searching", true);
                    Open.Play();
                    break;
                case RollerState.ARMING:
                    animator.SetBool("Closed", false);
                    animator.SetBool("Rolling", true);
                    animator.SetBool("Searching", false);
                    stateTimeLeft = 0.25f;
                    Arm.Play();
                    break;
                case RollerState.ROLLING:
                    stateTimeLeft = 1;
                    animator.SetBool("Closed", false);
                    animator.SetBool("Rolling", true);
                    animator.SetBool("Searching", false);
                    Roll.Play(0.25f);
                    break;
            }
        }

        /* should check from GameNetworkManager.Instance.LocalPlayer? Jak u springmana */
        PlayerControllerB PlayerVisible()
        {            

            //check first eye
            PlayerControllerB[] allPlayersInLineOfSight = GetAllPlayersInLineOfSight(30f, 70, eye, 3f);
            if(allPlayersInLineOfSight!= null && allPlayersInLineOfSight.Length > 0)
            {
                usingFirstEye = true;
                return allPlayersInLineOfSight[0];
            }


            //than second eye
            allPlayersInLineOfSight = GetAllPlayersInLineOfSight(30f, 70, secondEye, 3f);
            if (allPlayersInLineOfSight != null && allPlayersInLineOfSight.Length > 0)
            {                
                usingFirstEye = false;
                return allPlayersInLineOfSight[0];
            }

            //none found
            return null;
        }




        void FocusEyeOnPlayer(Transform player)
        {
            
            eye.LookAt(player.position + new Vector3(0, 1, 0));
            if (!usingFirstEye)
            {
                //revert rot
            }
            
            material.SetFloat("_EyeOpeness", Mathf.Lerp(material.GetFloat("_EyeOpeness"),-0.6f, pupilSpeed));
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);            
            if (agressive && !(timeSinceHittingPlayer >= 0f))
            {
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
                if (playerControllerB != null)
                {
                    timeSinceHittingPlayer = 0.5f;
                    playerControllerB.DamagePlayer(90, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 2);
                    playerControllerB.JumpToFearLevel(1f);
                }
            }
        }

        public void WallSignal()
        {            
            if (rollerState == RollerState.ROLLING && stateTimeLeft <= 0)
            {
                hitWall = true;
            }
        }
        
    }
}
