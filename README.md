# 🌌 Memory Journey Vault

**Memory Journey Vault** is a futuristic, sci-fi-themed memory timeline application. It allows you to store, organize, and revisit your personal milestones in a glowing, neon-styled interface.

The entire application is built as a **single HTML file** containing all HTML, CSS, and JavaScript. It runs completely offline in your browser, keeping your memories private and secure using `localStorage`.

---

## ✨ Features

- **🛸 Winding Path Timeline:** A dynamic, auto-generated S-curve timeline that adapts to your screen (Horizontal on desktop, vertical on mobile).
- **🔒 Privacy Lock Screen:** Secure your vault with a custom numeric passcode.
- **🖼️ Image Support & Compression:** Add thumbnail images to your memories. Images are automatically compressed using HTML5 Canvas to save storage space.
- **🏷️ Category Labels:** Create custom color-coded labels to organize your milestones.
- **🎨 Customization:** Personalize the vault with custom accent colors and background images/colors.
- **💾 Data Management:** Export your entire vault as a JSON backup file and restore it on any device.
- **📱 Fully Responsive:** Seamlessly transitions from a desktop horizontal scroll view to a mobile-friendly vertical feed.
- **🧭 Year Scrubber:** A built-in minimap navigation tool to quickly jump between years (desktop).

---

## 🚀 Getting Started

Since this is a standalone web application without backend requirements, getting started is incredibly simple!

### Installation
1. Clone this repository or download the ZIP file.
   ```bash
   git clone https://github.com/your-username/memory-journey-vault.git
   ```
2. Open the directory.
3. Double-click the `index.html` file to open it in your preferred web browser.

*That's it! No servers, no Node.js, no dependencies.*

---

## 🛠️ Technologies Used

- **HTML5:** Semantic structure and File APIs.
- **CSS3:** Custom properties (variables), Flexbox/Grid layouts, glassmorphism, animations, and media queries.
- **Vanilla JavaScript (ES6+):** DOM manipulation, Canvas API for image compression, Intersection Observer for scroll animations.
- **FontAwesome:** For UI icons.
- **Google Fonts:** `Orbitron` (Display) and `Rajdhani` (Body).

---

## 🗄️ Data Storage & Privacy

Your data never leaves your device. 
- All memories, images (Base64 compressed), labels, and settings are saved directly in your browser's **Local Storage** (`localStorage`).
- **Storage Limits:** Most browsers limit local storage to around 5MB. The built-in image compressor ensures you can store many memories, but it is highly recommended to periodically **Export a Backup** of your vault via the Control Panel.

---

## ⚙️ How to Use

1. **Add a Memory:** Click the glowing `+` Floating Action Button at the bottom right.
2. **Fill Details:** Add a title, date, description, location, label, and an image. 
3. **Navigate:** Scroll right/left on desktop or up/down on mobile. Use the Year Scrubber at the bottom to jump to specific times.
4. **Settings:** Click the gear icon `⚙️` in the top right to access the Control Panel. Here you can:
   - Set or remove your Privacy Passcode.
   - Manage Category Labels.
   - Export/Import JSON backups.
   - Change Theme Colors & Backgrounds.

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! 
Feel free to check the [issues page](../../issues).

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📝 License

Distributed under the MIT License. See `LICENSE` for more information.
