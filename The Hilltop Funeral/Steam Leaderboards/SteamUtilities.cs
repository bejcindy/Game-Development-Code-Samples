using UnityEngine;
using System.Collections;
using Steamworks;

// Used Steamworks Template from https://steamworks.github.io/

public class SteamUtilities : MonoBehaviour {
	private Vector2 m_ScrollPos;
	private Texture2D m_Image;
	private bool m_GameLauncherMode;

	protected Callback<IPCountry_t> m_IPCountry;
	protected Callback<LowBatteryPower_t> m_LowBatteryPower;
	//protected Callback<SteamAPICallCompleted_t> m_SteamAPICallCompleted;
	protected Callback<SteamShutdown_t> m_SteamShutdown;
	protected Callback<GamepadTextInputDismissed_t> m_GamepadTextInputDismissed;
	protected Callback<AppResumingFromSuspend_t> m_AppResumingFromSuspend;
	protected Callback<FloatingGamepadTextInputDismissed_t> m_FloatingGamepadTextInputDismissed;
	protected Callback<FilterTextDictionaryChanged_t> m_FilterTextDictionaryChanged;

	private CallResult<CheckFileSignature_t> OnCheckFileSignatureCallResult;

	public void OnEnable() {
		m_IPCountry = Callback<IPCountry_t>.Create(OnIPCountry);
		m_LowBatteryPower = Callback<LowBatteryPower_t>.Create(OnLowBatteryPower);
		m_SteamShutdown = Callback<SteamShutdown_t>.Create(OnSteamShutdown);
		m_AppResumingFromSuspend = Callback<AppResumingFromSuspend_t>.Create(OnAppResumingFromSuspend);
		m_FloatingGamepadTextInputDismissed = Callback<FloatingGamepadTextInputDismissed_t>.Create(OnFloatingGamepadTextInputDismissed);
		m_FilterTextDictionaryChanged = Callback<FilterTextDictionaryChanged_t>.Create(OnFilterTextDictionaryChanged);

		OnCheckFileSignatureCallResult = CallResult<CheckFileSignature_t>.Create(OnCheckFileSignature);
	}

	public static Texture2D GetSteamImageAsTexture2D(int iImage) {
		Texture2D ret = null;
		uint ImageWidth;
		uint ImageHeight;
		bool bIsValid = SteamUtils.GetImageSize(iImage, out ImageWidth, out ImageHeight);

		if (bIsValid) {
			byte[] Image = new byte[ImageWidth * ImageHeight * 4];

			bIsValid = SteamUtils.GetImageRGBA(iImage, Image, (int)(ImageWidth * ImageHeight * 4));
			if (bIsValid) {
				ret = new Texture2D((int)ImageWidth, (int)ImageHeight, TextureFormat.RGBA32, false, true);
				ret.LoadRawTextureData(Image);
				ret.Apply();
			}
		}

		return ret;
	}

	void OnIPCountry(IPCountry_t pCallback) {
		Debug.Log("[" + IPCountry_t.k_iCallback + " - IPCountry]");
	}

	void OnLowBatteryPower(LowBatteryPower_t pCallback) {
		Debug.Log("[" + LowBatteryPower_t.k_iCallback + " - LowBatteryPower] - " + pCallback.m_nMinutesBatteryLeft);
	}
	

	void OnSteamShutdown(SteamShutdown_t pCallback) {
		Debug.Log("[" + SteamShutdown_t.k_iCallback + " - SteamShutdown]");
	}

	void OnCheckFileSignature(CheckFileSignature_t pCallback, bool bIOFailure) {
		Debug.Log("[" + CheckFileSignature_t.k_iCallback + " - CheckFileSignature] - " + pCallback.m_eCheckFileSignature);
	}
	

	void OnAppResumingFromSuspend(AppResumingFromSuspend_t pCallback) {
		Debug.Log("[" + AppResumingFromSuspend_t.k_iCallback + " - AppResumingFromSuspend]");
	}

	void OnFloatingGamepadTextInputDismissed(FloatingGamepadTextInputDismissed_t pCallback) {
		Debug.Log("[" + FloatingGamepadTextInputDismissed_t.k_iCallback + " - FloatingGamepadTextInputDismissed]");
	}

	void OnFilterTextDictionaryChanged(FilterTextDictionaryChanged_t pCallback) {
		Debug.Log("[" + FilterTextDictionaryChanged_t.k_iCallback + " - FilterTextDictionaryChanged] - " + pCallback.m_eLanguage);
	}
}