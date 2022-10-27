using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ConfigCat.Client;
public class FeatureFlag : MonoBehaviour{

    // Creating the ConfigCat client instance using the SDK Key

    public static FeatureFlag instance;
    private static ConfigCatClient client = new ConfigCatClient("<ADD YOUR SDK KEY HERE>");

    // Overriding the constructor so we can't create a new instance
    private FeatureFlag(){}

    // Creating a Singleton
    void Awake () {
		MakeSingleton ();
    }

    void MakeSingleton() {
		if (instance != null) {
			Destroy (gameObject);
		} else {
			instance = this;
		}
			DontDestroyOnLoad(gameObject);
	}	

    // Universal function called when attached to other components and can be used with different flag names
    public bool FetchFlag(string flagName){
    bool isFlagOn = client.GetValue(flagName, false);
        if (isFlagOn) 
        {
        print("Feature is ON");
            return true;
        }
        else{
        print("Feature is OFF");
            return false;
        }
    }
}