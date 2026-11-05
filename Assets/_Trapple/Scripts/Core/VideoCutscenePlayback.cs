using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Trapple.Core
{
    /// <summary>Plays the story videos full-screen without requiring edits to authored scenes.</summary>
    public static class VideoCutscenePlayback
    {
        private const string IntroPlayedKey = "Trapple.Cutscenes.IntroPlayed";
        private const string FinalePlayedKey = "Trapple.Cutscenes.WorldTwoFinalePlayed";

        public static void PlayOpeningOnce(Action onFinished)
        {
            if (PlayerPrefs.GetInt(IntroPlayedKey, 0) != 0)
            {
                onFinished?.Invoke();
                return;
            }

            PlayerPrefs.SetInt(IntroPlayedKey, 1);
            PlayerPrefs.Save();
            Play("Cutscenes/OpeningCutscene", onFinished);
        }

        public static void PlayWorldTwoFinaleOnce(Action onFinished)
        {
            if (PlayerPrefs.GetInt(FinalePlayedKey, 0) != 0)
            {
                onFinished?.Invoke();
                return;
            }

            PlayerPrefs.SetInt(FinalePlayedKey, 1);
            PlayerPrefs.Save();
            Play("Cutscenes/WorldTwoFinale", onFinished);
        }

        private static void Play(string resourcePath, Action onFinished)
        {
            var clip = Resources.Load<VideoClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogError("Cutscene video is missing from Resources: " + resourcePath);
                onFinished?.Invoke();
                return;
            }

            var host = new GameObject("Cutscene Playback");
            UnityEngine.Object.DontDestroyOnLoad(host);
            var playback = host.AddComponent<Playback>();
            playback.Begin(clip, onFinished);
        }

        private sealed class Playback : MonoBehaviour
        {
            private VideoPlayer player;
            private RenderTexture target;
            private RawImage image;
            private Action finished;
            private bool completed;

            public void Begin(VideoClip clip, Action onFinished)
            {
                finished = onFinished;

                var canvasObject = new GameObject("Cutscene Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                var background = new GameObject("Black Background", typeof(RectTransform), typeof(Image));
                background.transform.SetParent(canvasObject.transform, false);
                Stretch(background.GetComponent<RectTransform>());
                background.GetComponent<Image>().color = Color.black;

                var videoObject = new GameObject("Video", typeof(RectTransform), typeof(RawImage));
                videoObject.transform.SetParent(canvasObject.transform, false);
                image = videoObject.GetComponent<RawImage>();
                image.color = Color.white;

                target = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32)
                {
                    name = "Cutscene Video Target"
                };
                target.Create();
                image.texture = target;

                player = gameObject.AddComponent<VideoPlayer>();
                player.playOnAwake = false;
                player.isLooping = false;
                player.source = VideoSource.VideoClip;
                player.clip = clip;
                player.renderMode = VideoRenderMode.RenderTexture;
                player.targetTexture = target;
                player.audioOutputMode = VideoAudioOutputMode.Direct;
                player.skipOnDrop = true;
                player.waitForFirstFrame = true;
                player.loopPointReached += _ => Finish();
                player.errorReceived += (_, message) =>
                {
                    Debug.LogError("Cutscene playback failed: " + message);
                    Finish();
                };
                player.prepareCompleted += OnPrepared;
                player.Prepare();
            }

            private void OnPrepared(VideoPlayer source)
            {
                int width = Mathf.Max(1, (int)source.width);
                int height = Mathf.Max(1, (int)source.height);
                if (target.width != width || target.height != height)
                {
                    source.targetTexture = null;
                    target.Release();
                    target.width = width;
                    target.height = height;
                    target.Create();
                    source.targetTexture = target;
                }

                float aspect = (float)width / height;
                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(.5f, .5f);
                if ((float)Screen.width / Screen.height > aspect)
                {
                    rect.sizeDelta = new Vector2(Screen.height * aspect, Screen.height);
                }
                else
                {
                    rect.sizeDelta = new Vector2(Screen.width, Screen.width / aspect);
                }

                source.Play();
            }

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    Finish();
            }

            private void Finish()
            {
                if (completed) return;
                completed = true;
                var callback = finished;
                finished = null;
                if (player != null) player.Stop();
                if (target != null)
                {
                    target.Release();
                    Destroy(target);
                }
                Destroy(gameObject);
                callback?.Invoke();
            }

            private static void Stretch(RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }
    }
}
