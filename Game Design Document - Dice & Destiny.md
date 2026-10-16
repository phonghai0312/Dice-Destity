# **GAME DESIGN DOCUMENT (GDD)**

## **TỰA GAME: DICE & DESTINY (DICE BUILDER RPG)**

Nền tảng: PC (Unity Engine) | Ngôn ngữ: C\# | Thể loại: Roguelike, Turn-based Strategy

## **MỤC LỤC**

> * 1\. Tổng quan trò chơi (High Concept)  
> * 2\. Trải nghiệm người dùng & Điều khiển (UI/UX & Controls)  
> * 3\. Lối chơi & Cơ chế (Gameplay & Mechanics)  
> * 4\. Cấu trúc Màn chơi & Tiến trình (Level Design)  
> * 5\. Nghệ thuật & Âm thanh (Art & Audio)  
> * 6\. Kiến trúc Kỹ thuật (Technical Architecture)

## **1\. TỔNG QUAN TRÒ CHƠI**

### **1.1. Tóm tắt & Bối cảnh (Setting & Lore)**

*\[Bổ sung sau: Viết ngắn gọn về thế giới của Dice & Destiny, nhân vật chính là ai, tại sao lại dùng xúc xắc làm vũ khí để chiến đấu với quái vật và mục đích của hành trình là gì.\]*

### **1.2. Vòng lặp tổng thể (Meta-game Loop)**

Là một tựa game Roguelike, người chơi sẽ phải chinh phục các ải liên tiếp. Vòng lặp cơ bản bao gồm:

> * **Khởi tạo:** Bắt đầu hành trình với bộ trang bị túi xúc xắc cơ bản nhất.  
> * **Trận đấu:** Vượt qua các ải quái vật tuần tự.  
> * **Phần thưởng (Xưởng rèn):** Thu thập mặt xúc xắc mới và rèn (thay thế) các mặt yếu cũ.  
> * **Điều kiện kết thúc:** Thắng (Victory) nếu hạ gục Boss cuối (Ải 3). Thua (Defeat) nếu HP về 0, toàn bộ tiến trình sẽ reset lại từ đầu (cơ chế Permadeath).

## **2\. TRẢI NGHIỆM NGƯỜI DÙNG & ĐIỀU KHIỂN**

### **2.1. Sơ đồ điều hướng màn hình (Screen Flow)**

> * Menu Chính (Main Menu) \-\> Lựa chọn nhân vật (nếu có) \-\> Main Gameplay Loop.  
> * Main Gameplay Loop: Màn hình Chiến đấu (Combat Scene) \-\> Màn hình Xưởng rèn (Forge Scene) \-\> Màn hình Chiến đấu...

### **2.2. Cơ chế điều khiển chi tiết (Control Scheme)**

> * **Sử dụng kỹ năng:** Kéo thả (Drag & Drop) trực tiếp mặt xúc xắc từ khu vực của người chơi vào mục tiêu (Kẻ địch hoặc bản thân) trên màn hình.  
> * **Xem thông tin (Tooltip):** Hover chuột vào mặt xúc xắc hoặc icon hiệu ứng (Độc, Choáng) để hiển thị bảng chú thích chi tiết công dụng.  
> * **Gieo lại (Re-roll):** Click chuột trái để chọn (hoặc bỏ chọn) các viên xúc xắc bất lợi. Bấm nút "Re-roll" trên UI để tung lại các viên đã chọn (tốn tài nguyên R\_current).  
> * **Hoàn tác (Undo):** Bấm nút Undo hoặc phím tắt để hủy lệnh vừa kéo thả (chỉ áp dụng trước khi bấm kết thúc lượt).

### **2.3. Wireframe & Bản nháp giao diện (UI/UX)**

*\[Bổ sung sau: Chèn hình ảnh phác thảo từ Figma cho Màn hình Chiến đấu, vị trí đặt thanh máu, túi xúc xắc, intent của quái và giao diện Màn hình Xưởng rèn.\]*

## **3\. LỐI CHƠI & CƠ CHẾ CHÍNH**

### **3.1. Mô Hình Đối Tượng Thực Thể (Entity Models)**

Mọi đối tượng tham gia trận chiến (Người chơi và Kẻ địch) đều kế thừa từ lớp trừu tượng Character:

> * **Máu tối đa (HP\_max):** Ngưỡng sống sót cao nhất.  
> * **Máu hiện tại (HP\_current):** Biến động qua sát thương và hồi máu. Khi HP \= 0, trạng thái chuyển sang Dead.  
> * **Giáp ảo (Shield):** Hấp thụ sát thương trước HP, chu kỳ làm mới về 0 ở đầu mỗi lượt.  
> * **Túi hiệu ứng trạng thái (S):** Tập hợp các hiệu ứng phụ cộng dồn: Độc (Poison), Choáng (Stun), Suy yếu (Weak).

**Người chơi (Player)** mở rộng với:

> * Túi xúc xắc: Tập hợp các khối lục diện đang trang bị (thường n từ 2-3 viên).  
> * Lượt gieo lại (R\_current): Số lần cho phép re-roll. Mặc định R\_max \= 1\.  
> * Túi mảnh mặt rèn: Kho lưu trữ mặt xúc xắc thu thập sau mỗi ải.

**Kẻ địch (Enemy)** không dùng xúc xắc mà vận hành theo Hệ thống Ý định dự báo trước (Deterministic Intent):

> * Intent (Action Type, Value, Target): Báo trước hành động ở đầu lượt của người chơi (Tấn công, Thủ, Debuff, Charge).

### **3.2. Hệ Thống Xúc Xắc & Cơ Chế Tùy Biến (Dice-Crafting)**

Mỗi viên xúc xắc gồm đúng 6 mặt. Mỗi mặt định nghĩa bởi: Name, Icon, Type (Attack, Defend, Heal, Special), Value (V), Rarity.  
**Cơ chế Rèn (Crafting):** Khi qua màn, chọn 1 mặt mới ghi đè lên 1 mặt cũ của viên xúc xắc hiện tại. Mặt cũ bị hủy hoặc phân rã.

| Nhóm | Tên mặt | Cấp | Giá trị (V) | Quy tắc thực thi chi tiết   |
| :---- | :---- | :---- | :---- | :---- |
| Tấn công | Strike | Common | 2-4 | Gây V sát thương chuẩn lên mục tiêu. |
| Tấn công | Heavy Slash | Rare | 6-8 | Gây V sát thương; yêu cầu tiêu thụ thêm 1 mặt Khiên. |
| Tấn công | Vampiric | Epic | 3-4 | Gây V sát thương, đồng thời hồi floor(V/2) HP cho bản thân. |
| Phòng thủ | Block | Common | 2-5 | Cộng trực tiếp V điểm vào Shield. Tự hủy khi hết lượt. |
| Phòng thủ | Iron Wall | Rare | 4 | Cộng V Shield, bảo lưu 50% giáp sang lượt sau. |
| Hồi phục | Regenerate | Common | 2-4 | Hồi V điểm HP. Không vượt ngưỡng HP\_max. |
| Hồi phục | Purify | Rare | 0 | Giải trừ toàn bộ hiệu ứng bất lợi. |
| Trạng thái | Poison Sting | Rare | 2-3 | Gây 1 dmg và cộng V stack Poison vào mục tiêu. |
| Trạng thái | Shock / Stun | Epic | 1 | Đặt Choáng, vô hiệu hóa Intent địch ở lượt kế. |
| Đặc biệt | Blank (Miss) | Common | 0 | Không tạo hiệu ứng (mặt rủi ro cần tìm cách rèn đè). |

### **3.3. Quy Tắc Toán Học Chiến Đấu (Combat Resolution)**

> * **Suy yếu (Weak):** Sát thương thực tế nếu bị dính Weak \= max(1, D\_raw \* 0.75).  
> * **Giáp (Shield):** Sát thương đâm vào máu \= Sát thương thực tế \- Lượng giáp bị vỡ.  
> * **Hồi máu:** HP mới \= min(HP\_max, HP\_current \+ Lượng hồi).  
> * **Độc (Poison):** Trừ máu trực tiếp ở cuối mỗi round, stack độc giảm dần 1 đơn vị mỗi lượt.

### **3.4. Máy Trạng Thái Trận Đấu (Turn-based State Machine)**

Vòng lặp điều phối bởi một hệ thống máy trạng thái hữu hạn tuần tự:

> 1. **RoundStart:** Đặt lại Shield Player về 0\. Quái chọn Intent mới. Khôi phục lượt re-roll cho người chơi.  
> 2. **PlayerRoll:** Tung ngẫu nhiên các xúc xắc. Player được phép thao tác Re-roll nếu còn lượt.  
> 3. **PlayerAction:** Kéo thả xúc xắc dùng kỹ năng. Các lệnh đưa vào Command Stack (hỗ trợ Undo). Kết thúc bằng nút "End Turn".  
> 4. **EnemyExecution:** Kiểm tra Stun (bỏ qua lượt nếu có, gỡ Stun). Nếu bình thường, thực thi chính xác Intent đã báo trước.  
> 5. **RoundEnd:** Kích hoạt Độc (Poison) 2 bên. Kiểm tra HP (Victory/Defeat). Nếu chưa kết thúc, chuyển vòng lại RoundStart.

## **4\. CẤU TRÚC MÀN CHƠI (LEVEL PROGRESSION)**

| Ải đấu | Máu (HP) & Giáp | Chu kỳ hành vi Intent | Ý đồ thiết kế (Design Intent)   |
| :---- | :---- | :---- | :---- |
| Ải 1: Goblin Scout | 20 HP | T1: 3 dmg T2: 4 dmg T3: Buff 3 giáp | Làm quen tính toán giáp và dứt điểm mục tiêu yếu. |
| Ải 2: Armored Orc | 35 HP (+5 Shield) | T1: 6 dmg T2: Buff 6 giáp T3: 4 dmg \+ Độc 2 | Ép người chơi tích lũy giáp lớn hoặc dùng Độc bào máu qua khiên. |
| Ải 3: Minotaur Boss | 60 HP | T1: Tích tụ (Buff \+3 Atk) T2: Bổ củi (12 dmg) T3: Lan 6 dmg \+ Hồi 4 HP | Thử thách sống sót, yêu cầu tối ưu bộ xúc xắc (Choáng hoặc Giáp cực đại). |

## **5\. HƯỚNG ĐI NGHỆ THUẬT & ÂM THANH (ART & AUDIO)**

### **5.1. Phong cách đồ họa (Art Direction)**

*\[Bổ sung sau: Định hình phong cách 2D (Ví dụ: Pixel, Flat Design hay Dark Fantasy), tone màu chủ đạo, và các hệ thống hạt Particle System cho hiệu ứng chém, vỡ giáp.\]*

### **5.2. Danh sách Tài nguyên Âm thanh (Audio Assets)**

*\[Bổ sung sau: Liệt kê nhạc nền BGM cho ải thường/Boss, và danh sách SFX (tiếng xúc xắc lăn lạch cạch, tiếng kỹ năng chém/hồi máu, tiếng rung camera).\]*

## **6\. KIẾN TRÚC PHẦN MỀM KHUYẾN NGHỊ (UNITY C\#)**

> * **Mô hình Data-Driven:** Tách biệt dữ liệu và mã xử lý bằng ScriptableObject. Tạo danh mục DiceFaceData để dễ dàng cân bằng game.  
> * **Strategy Pattern cho Kỹ năng:** Tạo interface IDiceAction (phương thức Execute). Mỗi mặt xúc xắc triển khai một Strategy độc lập, loại bỏ rẽ nhánh switch-case cồng kềnh.  
> * **Observer Pattern cho Giao diện:** Giao tiếp giữa Logic lõi và Canvas UI thông qua System.Action (ví dụ: OnHPChanged), triệt tiêu sự phụ thuộc (Coupling) giữa logic và UI.  
> * **Phân tách Event System & State Machine:** Xử lý trạng thái chặt chẽ theo TurnStateMachine để quản lý luồng trận đấu mượt mà.