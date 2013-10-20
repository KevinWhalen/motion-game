using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
    public float health = 100f;                         // How much health the player has left.
    public float resetAfterDeathTime = 5f;              // How much time from the player dying to the level reseting.
    public AudioClip deathClip;                         // The sound effect of the player dying.
    
    
   
    private SceneFadeInOut sceneFadeInOut;              // Reference to the SceneFadeInOut script.
    private float timer;                                // A timer for counting to the reset of the level once the player is dead.
    private bool playerDead;                            // A bool to show if the player is dead or not.
    
    
    void Awake ()
    {
        // Setting up the references. 
        sceneFadeInOut = GameObject.FindGameObjectWithTag(Tags.fader).GetComponent<SceneFadeInOut>();
	}
    
    
    void Update ()
    {
        // If health is less than or equal to 0...
        if(health <= 0f)
        {
          
                // Otherwise, if the player is dead, call the PlayerDead and LevelReset functions.
                LevelReset();
            }
        
    }
    
    
    void PlayerDying ()
    {
        // The player is now dead.
        playerDead = true;
    }
    

    
    void LevelReset ()
    {
        // Increment the timer.
        timer += Time.deltaTime;
        
        //If the timer is greater than or equal to the time before the level resets...
        if(timer >= resetAfterDeathTime)
            // ... reset the level.
            sceneFadeInOut.EndScene();
    }
    
    
    public void TakeDamage (float amount)
    {
        // Decrement the player's health by amount.
        health -= amount;
    }
}
