using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonControler : MonoBehaviour{
    
    public FeatureFlag featureFlag;
    [SerializeField] private string flagname = "istestscene"; 

    public void playButton()
    {
        bool flag = featureFlag.FetchFlag(flagname);
        if (flag)
        {
            SceneManager.LoadScene("FlagOnScene");
        }
        else if (!flag)
        {
            SceneManager.LoadScene("FlagOffScene");
        }
    }
}
