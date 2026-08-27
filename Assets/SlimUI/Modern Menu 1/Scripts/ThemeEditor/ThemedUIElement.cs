using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SlimUI.ModernMenu{
	[System.Serializable]
	public class ThemedUIElement : ThemedUI {
		[Header("Parameters")]
		Color outline;
		Image image;
		GameObject message;
		public enum OutlineStyle {solidThin, solidThick, dottedThin, dottedThick};
		public bool hasImage = false;
		public bool isText = false;

		protected override void OnSkinUI(){
			base.OnSkinUI();

			if (this == null || gameObject == null) return;
			if (themeController == null) return;

			if(hasImage){
				if (image == null)
				{
					image = GetComponent<Image>();
				}

				if (image != null)
				{
					image.color = themeController.currentColor;
				}
			}

			message = gameObject;

			if(isText && message != null){
				TMP_Text textComponent = message.GetComponent<TMP_Text>();
				if (textComponent != null)
				{
					textComponent.color = themeController.textColor;
				}
			}
		}
	}
}