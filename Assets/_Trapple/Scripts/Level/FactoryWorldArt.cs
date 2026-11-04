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

            SetSprite("Gear housing", "FactoryCrate");
            SetSprite("Boiler window", "FactoryFurnace");
            SetSprite("Turning machine cog", "GearWall");
            SetSprite("Jumping conveyor", "FactoryConveyor");
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

        private static void SetSprite(string objectName, string resourceName)
        {
            var sprite = Resources.Load<Sprite>("Factory/" + resourceName);
            if (sprite == null) return;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.gameObject.name != objectName) continue;
                renderer.sprite = sprite;
                renderer.color = Color.white;
                if (objectName == "Jumping conveyor")
                    renderer.transform.localScale = new Vector3(renderer.transform.localScale.x, .65f, 1f);
            }
        }

        private static bool IsPhotoSprite(string spriteName) =>
            spriteName.StartsWith("Factory") || spriteName == "Gear_0" || spriteName == "GearWall_0" ||
            spriteName == "FallingScrap_0" || spriteName == "DropWarning_0";
    }
}
