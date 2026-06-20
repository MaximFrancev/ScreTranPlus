# ![](ScreTranLogoSmall.png) ScreTran+

A minimalist, highly-optimized screen capture translator powered by local **PaddleOCR** and **Gemini 3.1 Flash-Lite**.

---

## System Requirements

1. Before running the application for the first time, make sure you have the [Visual C++ Redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe) package installed.
2. Your CPU must support [AVX](https://en.wikipedia.org/wiki/Advanced_Vector_Extensions) instructions (required for local PaddleOCR and ONNX runtime to work properly).

---

## Features

*   **Multiple Capture Areas**: Create and adjust multiple screen crop zones simultaneously. Perfect for split game UIs, visual novels, or dialogue boxes in RPGs.
*   **Gemini 3.1 Flash-Lite Integration**: Translate text using the latest low-latency, highly accurate Gemini model.
*   **Gemini Vision Mode**: Sends screenshots directly to Gemini for multimodal OCR and natural context-aware translation in a single API call, bypassing local resources.
*   **Local OCR Pre-Filter**: Runs local PaddleOCR first to detect text. If no text is on screen (e.g. in dark locations/night scenes), it skips calling Gemini, saving 100% of your free API limit.
*   **Dialogue Color Filter**: OpenCV-powered pixel analysis. Only captures and translates if the crop area matches a specified dialogue box background color (e.g. #000000).
*   **Manual Trigger Mode**: Disable automatic timer and translate on demand with a simple custom hotkey.
*   **International Support**: Clean English interface with support for translating into Russian, Ukrainian, Spanish, German, French, and Japanese.

## How to Use

1. Download the latest release from the [Releases](https://github.com/MaximFrancev/ScreTranPlus/releases) page.
2. Run `ScreTran.exe` (no installation required, fully portable).
3. **(Optional)** If you plan to use **Gemini** or **Gemini Vision** translators:
   * Generate a free API key at [Google AI Studio](https://aistudio.google.com/).
   * Paste your key into the **Gemini API Key** field in the application.
   *(Note: Legacy engines like Google Translate work out of the box and do not require any API keys).*
4. Set up your capture areas, select your translator/target language, and press **Start!** (or use the hotkey).

## Alternative Solutions
If this application doesn't fit your needs, you can check out these awesome open-source alternatives:
- [Translumo](https://github.com/Danily07/Translumo/)
- [LunaTranslator](https://github.com/Zonbe/LunaTranslator)
- [Lookupper](https://lookupper.ru/)

## Credits & Acknowledgement

*   **AI Assistance**: ScreTran+ has been developed with the help of AI.
