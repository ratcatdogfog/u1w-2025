using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class TransitionController : MonoBehaviour
{
    [SerializeField] Image backgroundImage;
    [SerializeField] CanvasGroup uiFadeGroup;
    [SerializeField] bool alsoFadeImageColor = true;
    
    [Header("Initial Values")]
    [Range(0f, 1f)] [SerializeField] float initialProgress = 1f;
    [Range(0f, 1f)] [SerializeField] float initialImageAlpha = 1f;
    [Range(0f, 1f)] [SerializeField] float initialCanvasGroupAlpha = 1f;
    
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
            Debug.LogError($"[TransitionController] backgroundImageまたはMaterialが設定されていません！ GameObject: {gameObject.name}");
            return;
        }

        _id = Shader.PropertyToID(progressRef);
        // マテリアルのインスタンスを作成（各TransitionControllerが独立したマテリアルを持つように）
        _mat = new Material(backgroundImage.material);
        backgroundImage.material = _mat; // Imageにインスタンスを設定（元のマテリアルアセットは変更されない）
        _originalImageColor = backgroundImage.color;

        if (!_mat.HasProperty(_id))
        {
            Debug.LogWarning($"[TransitionController] マテリアルにプロパティ '{progressRef}' が見つかりません。 GameObject: {gameObject.name}");
        }

        // 初期値を設定
        SetInitialValues();
    }

    void SetInitialValues()
    {
        if (_mat == null || backgroundImage == null) return;

        // Progressの初期値を設定
        if (_mat.HasProperty(_id))
        {
            _mat.SetFloat(_id, initialProgress);
        }

        // ImageのColorのalpha初期値を設定
        if (alsoFadeImageColor)
        {
            Color c = backgroundImage.color;
            c.a = initialImageAlpha;
            backgroundImage.color = c;
        }

        // CanvasGroupのalpha初期値を設定
        if (uiFadeGroup)
        {
            uiFadeGroup.alpha = initialCanvasGroupAlpha;
        }
    }

    public void ResetState()
    {
        _running = false;
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }

        // 初期値にリセット
        SetInitialValues();
    }

    public void PlayToBlack(Action onComplete = null)
    {
        if (_mat == null || backgroundImage == null)
        {
            Debug.LogWarning($"[TransitionController] PlayToBlack: _matまたはbackgroundImageがnullです。 GameObject: {gameObject.name}");
            onComplete?.Invoke();
            return;
        }

        if (_running)
        {
            Debug.LogWarning($"[TransitionController] PlayToBlack: 既に遷移が実行中です。 GameObject: {gameObject.name}");
            return;
        }

        // 1→0への遷移なので、現在の値が0の場合はエラー
        float currentProgress = _mat.HasProperty(_id) ? _mat.GetFloat(_id) : 1f;
        float currentImageAlpha = alsoFadeImageColor ? backgroundImage.color.a : 1f;
        
        const float epsilon = 0.01f; // 浮動小数点数の誤差を考慮
        if (currentProgress < epsilon || currentImageAlpha < epsilon)
        {
            Debug.LogError($"[TransitionController] PlayToBlack: 不正な値です。1→0への遷移を試みましたが、現在の値が既に0に近い値です。 Progress: {currentProgress}, ImageAlpha: {currentImageAlpha}, GameObject: {gameObject.name}");
            onComplete?.Invoke();
            return;
        }

        _running = true;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoPlay(progressTo: 0f, alphaTo: 0f, onComplete));
    }

    public void PlayFromBlack(Action onComplete = null)
    {
        if (_mat == null || backgroundImage == null)
        {
            Debug.LogWarning($"[TransitionController] PlayFromBlack: _matまたはbackgroundImageがnullです。 GameObject: {gameObject.name}");
            onComplete?.Invoke();
            return;
        }

        if (_running)
        {
            Debug.LogWarning($"[TransitionController] PlayFromBlack: 既に遷移が実行中です。 GameObject: {gameObject.name}");
            return;
        }

        // 0→1への遷移なので、現在の値が1の場合はエラー
        float currentProgress = _mat.HasProperty(_id) ? _mat.GetFloat(_id) : 0f;
        float currentImageAlpha = alsoFadeImageColor ? backgroundImage.color.a : 0f;
        
        const float epsilon = 0.01f; // 浮動小数点数の誤差を考慮
        if (currentProgress > (1f - epsilon) || currentImageAlpha > (1f - epsilon))
        {
            Debug.LogError($"[TransitionController] PlayFromBlack: 不正な値です。0→1への遷移を試みましたが、現在の値が既に1に近い値です。 Progress: {currentProgress}, ImageAlpha: {currentImageAlpha}, GameObject: {gameObject.name}");
            onComplete?.Invoke();
            return;
        }

        _running = true;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoPlay(progressTo: 1f, alphaTo: 1f, onComplete));
    }

    IEnumerator CoPlay(float progressTo, float alphaTo, Action onComplete)
    {
        if (_mat == null || backgroundImage == null)
        {
            Debug.LogError($"[TransitionController] CoPlay: _matまたはbackgroundImageがnullです。 GameObject: {gameObject.name}");
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
