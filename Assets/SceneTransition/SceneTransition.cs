using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class TransitionController : MonoBehaviour
{
    [SerializeField] Image backgroundImage;
    [SerializeField] CanvasGroup uiFadeGroup;
    [SerializeField] bool alsoFadeImageColor = true;
    
    [Header("Duration Settings")]
    [SerializeField] float progressDuration = 1f;
    [SerializeField] float uiFadeGroupDuration = 1f;

    string progressRef = "_Progress";
    Material _mat;
    int _id;
    Coroutine _co;
    bool _running;
    Color _originalImageColor;

    void Awake()
    {
        if (backgroundImage == null || backgroundImage.material == null)
        {
            Debug.LogError("backgroundImageまたはMaterialが設定されていません！");
            return;
        }

        _id = Shader.PropertyToID(progressRef);
        _mat = new Material(backgroundImage.material);
        backgroundImage.material = _mat;
        _originalImageColor = backgroundImage.color;

        if (!_mat.HasProperty(_id))
        {
            Debug.LogWarning($"マテリアルにプロパティ '{progressRef}' が見つかりません。");
        }

        ResetState();
    }

    public void ResetState()
    {
        _running = false;
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }

        if (_mat == null || backgroundImage == null) return;

        if (_mat.HasProperty(_id))
        {
            _mat.SetFloat(_id, 1f);
        }

        if (alsoFadeImageColor)
        {
            Color c = _originalImageColor;
            c.a = 1f;
            backgroundImage.color = c;
        }

        if (uiFadeGroup) uiFadeGroup.alpha = 1f;
    }

    public void PlayToBlack(Action onComplete = null)
    {
        if (_mat == null || backgroundImage == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (_running) return;

        _running = true;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoPlay(progressTo: 0f, alphaTo: 0f, onComplete));
    }

    IEnumerator CoPlay(float progressTo, float alphaTo, Action onComplete)
    {
        if (_mat == null || backgroundImage == null)
        {
            _running = false;
            onComplete?.Invoke();
            yield break;
        }

        float progressFrom = _mat.HasProperty(_id) ? _mat.GetFloat(_id) : 1f;
        float alphaFrom = uiFadeGroup ? uiFadeGroup.alpha : 1f;
        float imageAlphaFrom = alsoFadeImageColor ? backgroundImage.color.a : 1f;

        // 各要素の経過時間を個別に管理
        float progressElapsed = 0f;
        float uiFadeGroupElapsed = 0f;

        while (progressElapsed < progressDuration || 
               uiFadeGroupElapsed < uiFadeGroupDuration)
        {
            float deltaTime = Time.deltaTime;
            progressElapsed += deltaTime;
            uiFadeGroupElapsed += deltaTime;

            // Progressの更新
            if (_mat.HasProperty(_id) && progressElapsed < progressDuration)
            {
                float normalizedTime = Mathf.Clamp01(progressElapsed / progressDuration);
                float currentProgress = Mathf.Lerp(progressFrom, progressTo, normalizedTime);
                _mat.SetFloat(_id, currentProgress);
            }
            else if (_mat.HasProperty(_id) && progressElapsed >= progressDuration)
            {
                _mat.SetFloat(_id, progressTo);
            }

            // ImageのColorのalpha更新（progressDurationと同じタイミング）
            if (alsoFadeImageColor && progressElapsed < progressDuration)
            {
                float normalizedTime = Mathf.Clamp01(progressElapsed / progressDuration);
                float currentImageAlpha = Mathf.Lerp(imageAlphaFrom, alphaTo, normalizedTime);
                Color c = backgroundImage.color;
                c.a = currentImageAlpha;
                backgroundImage.color = c;
            }
            else if (alsoFadeImageColor && progressElapsed >= progressDuration)
            {
                Color c = backgroundImage.color;
                c.a = alphaTo;
                backgroundImage.color = c;
            }

            // CanvasGroupのalpha更新
            if (uiFadeGroup && uiFadeGroupElapsed < uiFadeGroupDuration)
            {
                float normalizedTime = Mathf.Clamp01(uiFadeGroupElapsed / uiFadeGroupDuration);
                float eased = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
                uiFadeGroup.alpha = Mathf.Lerp(alphaFrom, alphaTo, eased);
            }
            else if (uiFadeGroup && uiFadeGroupElapsed >= uiFadeGroupDuration)
            {
                uiFadeGroup.alpha = alphaTo;
            }

            yield return null;
        }

        // 最終値を確実に設定
        if (_mat.HasProperty(_id)) _mat.SetFloat(_id, progressTo);
        if (alsoFadeImageColor)
        {
            Color c = backgroundImage.color;
            c.a = alphaTo;
            backgroundImage.color = c;
        }
        if (uiFadeGroup) uiFadeGroup.alpha = alphaTo;

        _running = false;
        onComplete?.Invoke();
    }
}
