    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;


    public class Zones : MonoBehaviour
    {
        public GameObject cameraGameObject;
        public Controller2D controller;
        public GameObject direction;
        public TextMeshProUGUI cueText;
        public float zoomOut = 1f;
        public int zoomIn;

        float initialCameraSize = 0;
        float step = 1;
        string helperOriginalText;

        Vector3 position;
        // GameObject[,] collisionParticle; If we want to declare an array of array, this would be the way to go
        GameObject[] collisionParticle;

        // Music zone support
        public float musicFadeDuration = 1.5f;

        public AudioClip previousMusicClip;
        public bool hasStoredPreviousMusic;

        public enum MusicZoneMode
        {
            TemporaryOverride,   // enter: switch to zone clip, exit: revert to previous
            OneWaySet            // enter: switch to zone clip, exit: do nothing
        }

        [Header("Music zone (optional)")]
        public MusicZoneMode musicMode = MusicZoneMode.TemporaryOverride;

        // Start is called before the first frame update
        void Start()
        {
            if (this.transform.parent.name == "Help"){
                helperOriginalText = cueText.text;
            }
        }
        
        // Update is called once per frame
        void Update()
        {
            
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {       
                if (this.transform.parent.name == "Zoom"){
                    controller.zoneInfo.insideZoomZone = true;
                    controller.zoneInfo.zoomOut = zoomOut;
                    controller.zoneInfo.exitZone = false;
                }
                if (this.transform.parent.name == "Help"){
                    switch (this.transform.name){
                        case "Sprint":
                            cueText.text = "Sprint: Double press the forward arrow";
                            break;
                        case "Wall jump":
                            cueText.text = "Wall jump:\nJump towards the wall to climb.\nJump away from the wall to leap away.";
                            break;
                        case "Down through":
                            cueText.text = "Press the down arrow to go through the floor.";
                            break;
                        case "Climb ladder":
                            cueText.text = "Climb the ladder by pressing the up arrow.";
                            break;
                        case "Get double jump":
                            cueText.text = "Keep jump pressed while bouncing on an ennemy to reach higher and get double-jump!";
                            break;
                        case "Talk to people":
                            cueText.text = "CTRL to start a conversation.";
                            break;
                    }
                }
                if (this.transform.parent.name == "Direction"){
                    controller.activeDirectionZones++;
                    switch (this.transform.name){
                        case "Up":
                            direction.transform.rotation = Quaternion.Euler(0, 0, 90);
                            break;

                        case "Left":
                            direction.transform.rotation = Quaternion.Euler(0, 0, 180);
                            break;

                        case "Down":
                            direction.transform.rotation = Quaternion.Euler(0, 0, 270);
                            break;
                    }
                }  
                if (this.transform.parent.name == "Speed"){
                    print("Speed down!");
                }
                if (this.transform.parent.name == "Music")
                {
                    if (MusicManager.I != null)
                    {
                        var audio = transform.GetComponent<AudioSource>();
                        if (audio != null && audio.clip != null)
                        {
                            if (musicMode == MusicZoneMode.TemporaryOverride)
                            {
                                // Remember what was playing before entering this zone
                                previousMusicClip = MusicManager.I.CurrentClip;
                                hasStoredPreviousMusic = previousMusicClip != null;
                            }
                            else // OneWaySet
                            {
                                // Do not revert when exiting this zone
                                previousMusicClip = null;
                                hasStoredPreviousMusic = false;
                            }

                            // Transition to the zone music
                            MusicManager.I.TransitionTo(audio.clip, musicFadeDuration, loop: true);
                        }
                        else
                        {
                            Debug.LogWarning($"[Zones] Music object '{transform.name}' has no AudioSource or no clip assigned.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[Zones] No MusicManager instance found.");
                    }
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {            
                if (this.transform.parent.name == "Zoom")
                {
                    controller.zoneInfo.insideZoomZone = false;
                    controller.zoneInfo.zoomOut = 0f;     // no zoom zone active anymore
                    controller.zoneInfo.exitZone = true;  // now camera can zoom back to default
                }
                if (this.transform.parent.name == "Direction"){
                    controller.activeDirectionZones--;
                    if (controller.activeDirectionZones == 0){
                        direction.transform.rotation = Quaternion.Euler(0, 0, 0);
                    }
                }
                if (this.transform.parent.name == "Help"){
                    cueText.text = helperOriginalText;
                }
                if (this.transform.parent.name == "Speed"){
                    print("Speed up!");
                }
                if (this.transform.parent.name == "Music")
                {
                    // Only TemporaryOverride zones revert on exit
                    if (musicMode == MusicZoneMode.TemporaryOverride &&
                        MusicManager.I != null &&
                        hasStoredPreviousMusic &&
                        previousMusicClip != null)
                    {
                        MusicManager.I.TransitionTo(previousMusicClip, musicFadeDuration, loop: true);
                    }
                    // OneWaySet: do nothing on exit
                }
            }
        }
    }
