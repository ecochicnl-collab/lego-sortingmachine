using UnityEngine;
using TMPro;
using System.Collections;

public class screenmanager : MonoBehaviour
{
    public RectTransform screena;
    public RectTransform screenb;

    public float  slideSnelheid = 10f;
    public float ruimteErtussen = 50f;
    public float extraVerschuiving = 100f;

    private Vector2 screenATargetPos;
    private Vector2 screenBTargetPos;
    private float offset;

    public TextMeshProUGUI tekstvak;
    
    private bool screenAActive = true;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tekstvak.text = "Screen A";
       if (screena == null || screenb == null) return;
        offset = screena.rect.width + ruimteErtussen;
        screenATargetPos = new Vector2(extraVerschuiving, 0f);
        screenBTargetPos = new Vector2(offset + extraVerschuiving, 0f);

        screena.anchoredPosition = screenATargetPos;
        screenb.anchoredPosition = screenBTargetPos;
    }

    // Update is called once per frame
    void Update()
    {
        if (screena == null || screenb == null) return;

        screena.anchoredPosition = Vector2.Lerp(screena.anchoredPosition, screenATargetPos, Time.deltaTime * slideSnelheid);
        screenb.anchoredPosition = Vector2.Lerp(screenb.anchoredPosition, screenBTargetPos, Time.deltaTime * slideSnelheid);
    }

    public void wisselscreen()
    {
        screenAActive = !screenAActive;

        if (screenAActive)
        {
            tekstvak.text = "Screen A";
            screenATargetPos = new Vector2(extraVerschuiving, 0f);
            screenBTargetPos = new Vector2(offset + extraVerschuiving, 0f);
        }
        else
        {
            tekstvak.text = "Screen B";
            screenATargetPos = new Vector2(offset + extraVerschuiving, 0f);
            screenBTargetPos = new Vector2(extraVerschuiving, 0f);
        }
    }
}

