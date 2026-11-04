using UnityEngine;

namespace Trapple.Level
{
    /// <summary>Applies the supplied factory sprite sheet to the factory scene at runtime.</summary>
    public static class FactoryWorldArt
    {
        private const float BackdropHeight = 12f;
        private const float LevelLength = 670f;

        public static void Apply()
        {
            var backdrop = Resources.Load<Sprite>("Factory/FactoryBackdrop");
            var originalBackdrop = GameObject.Find("Factory interior");
            if (originalBackdrop != null)
            {
                var renderer = originalBackdrop.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.enabled = false;
            }

            if (backdrop != null) CreateBackdrop(backdrop);
            CreateCeilingPipes();
            CreateFactorySupports();

            SetSprite("Gear housing", "FactoryCrate");
            SetSprite("Boiler window", "FactoryFurnace");
            SetSprite("Turning machine cog", "GearWall");
            SetAlternatingSprite("Jumping conveyor", "FactoryConveyor", "FactoryPlatform");
            SetSprite("Gear powered blast door", "FactoryBlastDoor");
            SetSprite("Gear socket lamp", "Gear");
            SetSprite("Factory Gear", "Gear");
            SetSprite("Falling Scrap", "FallingScrap");
            SetSprite("Crusher teeth", "FallingScrap");
            SetSprite("Factory exit apple", "FactoryApple");

            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.gameObject.name == "Window mullion" || renderer.gameObject.name == "Back wall support" ||
                    renderer.gameObject.name == "Conveyor rail" || renderer.gameObject.name == "Overhead pipe")
                    renderer.enabled = false;
                if (renderer.gameObject.name != "Factory interior" &&
                    renderer.gameObject.name != "Window mullion" &&
                    renderer.sprite != null && IsPhotoSprite(renderer.sprite.name))
                    renderer.color = Color.white;
            }
        }

        private static void CreateBackdrop(Sprite sprite)
        {
            float width = BackdropHeight * sprite.bounds.size.x / sprite.bounds.size.y;
            int count = Mathf.CeilToInt(LevelLength / width);
            float start = 0f;
            for (int i = 0; i < count; i++)
            {
                var tile = new GameObject("Factory panorama " + (i + 1));
                tile.transform.position = new Vector3(start + width * .5f, 4.4f, 0f);
                tile.transform.localScale = new Vector3(width / sprite.bounds.size.x,
                    BackdropHeight / sprite.bounds.size.y, 1f);
                var renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = -31;
                renderer.flipX = i % 2 == 1;
                start += width;
            }
        }

        private static void CreateCeilingPipes()
        {
            var pipe = Resources.Load<Sprite>("Factory/FactoryPipe");
            if (pipe == null) return;
            foreach (float x in new[] { 76f, 270f, 470f })
            {
                var fixture = new GameObject("Factory hanging pipe lamp");
                fixture.transform.position = new Vector3(x, 8.55f, 0f);
                fixture.transform.localScale = new Vector3(1.1f / pipe.bounds.size.x, 1.9f / pipe.bounds.size.y, 1f);
                var renderer = fixture.AddComponent<SpriteRenderer>();
                renderer.sprite = pipe;
                renderer.sortingOrder = -10;
            }
        }

        private static void CreateFactorySupports()
        {
            var support = Resources.Load<Sprite>("Factory/FactoryPillar");
            if (support == null) return;
            foreach (float x in new[] { 24f, 190f, 355f, 520f })
            {
                var structure = new GameObject("Factory reference scaffold");
                structure.transform.position = new Vector3(x, 3.5f, 0f);
                structure.transform.localScale = new Vector3(4f / support.bounds.size.x,
                    4.7f / support.bounds.size.y, 1f);
                var renderer = structure.AddComponent<SpriteRenderer>();
                renderer.sprite = support;
                renderer.sortingOrder = -15;
            }
        }

        private static void SetAlternatingSprite(string objectName, string firstResource, string secondResource)
        {
            var first = Resources.Load<Sprite>("Factory/" + firstResource);
            var second = Resources.Load<Sprite>("Factory/" + secondResource);
            if (first == null && second == null) return;
            var matches = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (renderer.gameObject.name == objectName) matches.Add(renderer);
            matches.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            for (int i = 0; i < matches.Count; i++)
            {
                var renderer = matches[i];
                Sprite sprite = i % 2 == 0 ? first ?? second : second ?? first;
                var size = new Vector2(Mathf.Abs(renderer.transform.localScale.x), .65f);
                ApplySprite(renderer, sprite, size, true);
            }
        }

        private static void SetSprite(string objectName, string resourceName)
        {
            var sprite = Resources.Load<Sprite>("Factory/" + resourceName);
            if (sprite == null) return;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.gameObject.name != objectName) continue;
                var size = new Vector2(Mathf.Abs(renderer.transform.localScale.x),
                    Mathf.Abs(renderer.transform.localScale.y));
                ApplySprite(renderer, sprite, size, false);
            }
        }

        public static void FitSpriteToNominalSize(SpriteRenderer renderer, Vector2 size)
        {
            if (renderer != null && renderer.sprite != null)
                ApplySprite(renderer, renderer.sprite, size, false);
        }

        private static void ApplySprite(SpriteRenderer renderer, Sprite sprite, Vector2 desiredSize,
            bool fitColliderToSprite)
        {
            if (sprite == null || renderer == null) return;
            Vector3 oldScale = renderer.transform.localScale;
            Vector3 newScale = new Vector3(desiredSize.x / Mathf.Max(.001f, sprite.bounds.size.x),
                desiredSize.y / Mathf.Max(.001f, sprite.bounds.size.y), oldScale.z);
            float ratioX = Mathf.Abs(oldScale.x / newScale.x);
            float ratioY = Mathf.Abs(oldScale.y / newScale.y);

            foreach (var box in renderer.GetComponents<BoxCollider2D>())
            {
                Vector2 oldWorldSize = Vector2.Scale(box.size, new Vector2(Mathf.Abs(oldScale.x), Mathf.Abs(oldScale.y)));
                Vector2 newWorldSize = fitColliderToSprite ? desiredSize : oldWorldSize;
                box.size = new Vector2(newWorldSize.x / Mathf.Max(.001f, Mathf.Abs(newScale.x)),
                    newWorldSize.y / Mathf.Max(.001f, Mathf.Abs(newScale.y)));
                box.offset = Vector2.Scale(box.offset, new Vector2(ratioX, ratioY));
            }
            foreach (var circle in renderer.GetComponents<CircleCollider2D>())
            {
                if (fitColliderToSprite)
                    circle.radius = desiredSize.x * .5f / Mathf.Max(.001f, Mathf.Abs(newScale.x));
                else
                    circle.radius *= ratioX;
                circle.offset = Vector2.Scale(circle.offset, new Vector2(ratioX, ratioY));
            }

            renderer.sprite = sprite;
            renderer.transform.localScale = newScale;
            renderer.color = Color.white;
        }

        private static bool IsPhotoSprite(string spriteName) =>
            spriteName.StartsWith("Factory") || spriteName == "Gear_0" || spriteName == "GearWall_0" ||
            spriteName == "FallingScrap_0" || spriteName == "DropWarning_0";
    }
}
