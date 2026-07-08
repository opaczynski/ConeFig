<img width="100%" alt="header1" src="https://github.com/user-attachments/assets/4756f33c-f0b9-45ca-9cac-79f754e58821" />

[![Windows Support](https://img.shields.io/badge/OS-Windows%20Only-0078D4?style=for-the-badge&logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)

**ConeFig** is a powerful desktop utility that applies full-screen shaders directly to your primary display, allowing developers, designers, and accessibility researchers to experience their digital environments through the lens of various color vision deficiencies (CVD). 

Unlike software that only captures specific windows or relies on slow CPU-based filtering, ConeFig processes the entire screen at the hardware level. By leveraging low-latency GPU pipelines, it delivers a real-time emulation experience suitable for testing video games, complex user interfaces, and multimedia content.
<br><br>
<details>
<summary><h3>⚠️ System Requirements & Limitations (Click to expand)</h3></summary>
 
<b>Hardware-accelerated performance:</b> ConeFig utilizes native GPU shader pipelines to achieve hardware-level full-screen filtering. While it works across different graphics cards, using the application on lower-end or integrated GPUs (such as integrated Intel graphics) may cause minor drops in screen fluidness or framerate. For a completely flawless, zero-latency experience, a dedicated graphics card is recommended.
 
<h3>Known Windows & Full-Screen Limitations:</h3>
<ul>
<li><b>OS Interface Restrictions:</b> Due to core Windows architectural limitations, the shader pipeline cannot be applied to certain system-level UI elements (such as the secure UAC prompt screens or specific system overlays).</li>
<li><b>True Fullscreen Compatibility:</b> Applications and games running in exclusive <i>True Fullscreen</i> mode bypass the standard desktop composition layer, which may cause the shaders to stop rendering. To ensure consistent color emulation, it is highly recommended to run your games or apps in <b>Borderless Windowed</b> or <b>Windowed</b> mode.</li>
</ul>

</details>

---

<img width="100%" alt="header2" src="https://github.com/user-attachments/assets/46552fad-7ad1-4533-8c32-f17770420506" />

* **Full-Screen Hardware Emulation:** Applies shaders globally over all applications.
* **Comprehensive CVD Coverage:** Accurate mathematical models for all types of color blindness:
  * **Protanopia / Protanomaly** (red-blindness/weakness)
  * **Deuteranopia / Deuteranomaly** (green-blindness/weaknesss)
  * **Tritanopia / Tritanomaly** (blue-blindness/weakness)
  * **Monochromaticism** (L/M/S variants)
  * **Rod Monochromacy** (blindness of all cones)
* **Customization:** Adjust the degree of cone blindness and fully customize the rod monochromacy (edit constants in GLSL files).
* **Zero Performance Impact:** Runs entirely via optimized shaders on the GPU, maintaining high frame rates ideal for real-time UI tests.
* **Scalability:** The program detects shaders in the folder, so you can expand the program with your own shaders.
* **Minimalist UI:** Select a shader from the list, then switch between shaders using CTRL+ALT+LEFT/RIGHT shortcut.
<br><br>
<details>
<summary><h3>🖼 Examples of shader effects (Click to expand)</h3></summary>

<h3 align="center">Original:</h3>
<p align="center"><img width="50%" alt="playing_with_dog" src="https://github.com/user-attachments/assets/4343c9c0-1172-4d51-bcc2-3b16d4a6542a" /></p>
<h3 align="center">Deuteranopia:</h3>
<p align="center"><img width="50%" alt="deuteranopia" src="https://github.com/user-attachments/assets/cc136ff8-c465-498b-8fdd-3a726f7c8ccf" /></p>
<h3 align="center">Tritanopia:</h3>
<p align="center"><img width="50%" alt="tritanopia" src="https://github.com/user-attachments/assets/7ee3ca00-c2ad-475e-a5d2-c3b6f27283bf" /></p>
<h3 align="center">Monochromatism (L):</h3>
<p align="center"><img width="50%" alt="monochromatism_l" src="https://github.com/user-attachments/assets/847da0fd-f10e-4e8b-b4a0-c7121face203" /></p>
<h3 align="center">Rod monochromacy:</h3>
<p align="center"><img width="50%" alt="rod_monochromacy" src="https://github.com/user-attachments/assets/fc7ce0a4-b624-48cb-9c24-8e47ba192ee8" /></p>




</details>
---

## 🎨 Credits

The amazing Aria artwork used in these banners was created by **Kaibeexx**. If you love the style and want to commission your own graphics, check out her professional portfolio and availability here:

[![VGen](https://img.shields.io/badge/Commission_Me-VGen-FF5A5F?style=for-the-badge&logo=vgen&logoColor=white)](https://vgen.co/Kaibeexx)
