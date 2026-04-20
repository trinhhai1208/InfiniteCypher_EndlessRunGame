# Future City - Infinite 🏃‍♂️🏙️

**Future City Endless Run** là một dự án game 3D Endless Runner tốc độ cao được phát triển trên nền tảng Unity 3D (Universal Render Pipeline). Lấy cảm hứng từ thể loại Subway Surfers truyền thống, trò chơi mang đến cơ chế rượt đuổi bằng Boss kịch tính, hệ thống trượt/nhảy linh hoạt, và đặc biệt được tối ưu hóa cực hạn để chạy mượt mà trên nền tảng trình duyệt di động (WebGL Mobile) qua itch.io.

---

## 🌟 Chức Năng Nổi Bật (Features)

*   **Cơ chế Chuyển Làn (3-Lane Movement):** Vuốt cực nhạy để chuyển qua lại giữa 3 làn đường. Hỗ trợ hệ thống nhận diện Swipe linh hoạt, không vướng độ trễ.
*   **Hệ thống Rượt Đuổi (Boss Chase System):** Khi người chơi di chuyển lỗi (vấp mép xe buýt/xe hơi), Boss sẽ lập tức xuất hiện truy đuổi sát nút ở phía sau (Được thiết lập nội suy SmoothDamp siêu mượt). Nếu người chơi sống sót sau một khoản thời gian, Boss sẽ bỏ cuộc.
*   **Thể thức Đụng độ Phân cấp (Tiered Collision):** 
    *   **Nhảy lên nóc:** Cho phép nhảy an toàn lên đầu xe buýt/chướng ngại vật (*Jumpable*).
    *   **Va quẹt hông (Stumble):** Nếu chạy tạt ngang hông phương tiện giao thông (*VehicleStumble*), nhân vật sẽ bị loạng choạng và kích hoạt Boss Chase.
    *   **Va chạm trực diện (Fatal):** Game Over ngay lập tức.
*   **Vật Phẩm Hỗ Trợ (Power-ups):** 
    *   🧲 **Magnet (Nam châm):** Hút tự động mọi đồng xu phía trước với gia tốc dính thông minh, đảm bảo không bỏ sót bất kỳ đồng nào.
    *   🛡️ **Shield (Khiên bảo vệ):** Đỡ thay người chơi một mạng khi va chạm trực diện chướng ngại vật.
    *   ✨ **Multiplier (X Nhân điểm):** Tăng tốc độ ghi điểm số.
*   **Tối Ưu Chuyên Sâu WebGL:** Đồ họa URP tinh gọn, không Post-Processing dư thừa, giới hạn Shadow và nén Audio chuẩn Mono phục vụ thời gian tải siêu nhanh (Fast load time) trên Browser điện thoại.

---

## 🎮 Hướng Dẫn Điều Khiển (Controls)

Hệ thống điều khiển được thiết kế thân thiện cho cả Bàn phím máy tính và Màn hình cảm ứng:

| Thao Tác | Bàn Phím (PC) | Màn Hình Vuốt (Mobile / Web) |
| :--- | :--- | :--- |
| **Chuyển Trái/Phải** | `A` / `D` hoặc `←` / `→` | Vuốt ngón tay sang Trái / Phải |
| **Nhảy (Jump)** | `W`, `Space`, hoặc `↑` | Vuốt ngón tay lên trên |
| **Cúi / Lăn (Roll)** | `S` hoặc `↓` | Vuốt ngón tay xuống (khi ở mặt đất) |
| **Lao Nhanh (Dive)** | `S` hoặc `↓` | Vuốt ngón tay xuống (khi đang ở trên không) |

---

## 📂 Cấu Trúc Dự Án (Project Structure)

Dự án tuân thủ nghiêm ngặt quy tắc Component-based và chia tách Module rõ ràng:

*   **`Player/`**: Trái tim của Gameplay. `PlayerController.cs` xử lý vật lý (`Interpolate Rigidbody`), Animation và Nhận diện tương tác Vuốt/Phím.
*   **`Boss/`**: Logic truy đuổi. Gồm `BossChaseManager.cs` (điều hướng sự kiện khi vấp/nhảy) và `BossController.cs` (logic bám đuổi delay mượt sau lưng).
*   **`Obstacles/`**: Chứa component `ObstacleIdentity.cs`. Dùng Data Marker để xác định loại nguy hiểm thay vì so khớp thẻ Tag phần cứng truyền thống.
*   **`CameraFollow.cs`**: Quản lý Camera bằng LateUpdate, theo dõi mịn màng toạ độ nội suy của nhân vật, và tự động Zoom Out khi vào trạng thái Chase Mode.
*   **`Consumables/`**: Chứa hệ thống `PowerUpManager.cs` điều khiển Item Magnet, Shield. Quản lý trạng thái không dùng OnUpdate của từng đồng xu để lấy lại hiệu năng mà quét tập trung trên Player (OverlapSphereNonAlloc).

---

## 🔧 Phục Vụ Build WebGL Di Động (Deployment)

Nếu bạn là Developer tải source code này về để Build lên Web. Lưu ý các cấu hình bắt buộc:

1.  **Unity Player Settings:** Gắn Scripting Backend = `IL2CPP`. Strip Engine Code = `On` (Medium).
2.  **URP Settings:** Chuyển `Render Scale` vùng 0.75 - 0.85. Tắt MSAA. 
3.  **Shadows:** Giữ `Shadow Resolution` của Main Light ở mức `512` và tắt hoàn toàn `Additional Lights`. Tính toán bóng của Đồng xu nên chuyển MeshRenderer sang `Off`.
4.  **Audio Fix:** Toàn bộ nhạc và tiếng động phải Override cho WebGL chọn `Force To Mono` (Cắt nửa gánh nặng vi xử lý bộ nhớ ngầm).

---
*Developed & Optimized internally specifically for cross-platform Web browser experiences. 🚀*

---

## 📊 Benchmark Hiệu Năng WebGL (Ngày 5)

Dự án đã đạt được các chỉ số tối ưu chuyên sâu cho WebGL Mobile:
*   **GC Alloc:** 0 KB trong frame gameplay bình thường.
*   **Object Pooling:** Tái sử dụng 100% Coin và PowerUp UI (O(1) access).
*   **Memory Safety:** Addressables được release triệt để, ngăn chặn crash Memory trên trình duyệt di động.
*   **Jitter-free:** Camera và Player di chuyển mượt mà nhờ phối hợp LateUpdate và Interpolation.

