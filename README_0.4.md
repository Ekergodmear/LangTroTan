# Thanh Vân Tông — bản chuyển thể Unity 0.4.0

Tiếp tục từ Làng Tro Tàn 0.3, tham khảo Tứ Đại Bộ Châu — Tu Tiên Ký trong mã nguồn người dùng cung cấp tại Desktop/tu-tien-game. Đây là bản phát triển có thể chơi thử, chưa phải bản chuyển toàn bộ game web sang Unity.

## Đã thêm

- Chánh Điện, Đệ Tử Xá và Diễn Võ Trường có mô hình mái ngói nhiều tầng. Giữ các công trình sinh tồn từ 0.3, thêm Tàng Kinh Các, Tụ Linh Trận, Linh Mỏ, Dược Viên, Luyện Đan Đường, Luyện Khí Điện và Nhiệm Vụ Điện: tổng cộng 21 loại công trình, menu 3 trang.
- Có thể xây nhiều Đệ Tử Xá. Tổng giới hạn vẫn 80 công trình, dân tối đa 40 và vùng xây bán kính 24 ô. Từng nhà nâng tối đa cấp 3; nhà thường không vượt Chánh Điện. Mỗi cấp Đệ Tử Xá thêm 4 chỗ, Kho thêm 300 sức chứa, công trình sản xuất tăng 20% nhân dồn và thêm một chỗ làm. Công trình không có người làm tăng HP; Tụ Linh Trận tăng hiệu quả tu luyện theo cấp.
- Nâng cấp trả chi phí ngay, cần giữ một người rảnh, kéo dài 30/60 giây theo cấp hiện tại; Chánh Điện 60/120 giây. Trong lúc nâng vẫn hoạt động, hủy hoàn 80% vật tư trong giới hạn kho. Cấp 3 là trần bản thử nghiệm.
- Linh thạch, linh thảo, Trúc Cơ Đan, pháp khí, danh vọng, ba bậc tông môn; các xưởng cần người và nguyên liệu thật.
- Thu đồ từ người rảnh: không sinh thêm dân. Đệ tử có tên, trường phái, tư chất, cảnh giới/tiểu cảnh, tu vi, tâm ma, thương tích, pháp khí và công việc riêng. Tối đa 8 đệ tử ở Chánh Điện 2, 12 ở cấp 3, trong tổng dân số.
- Tự tu luyện, lên tiểu cảnh; người chơi quyết định đột phá đại cảnh giới. Luyện Khí có 9 tầng; các cảnh giới sau có Sơ Kỳ, Trung Kỳ, Hậu Kỳ, Đỉnh Phong. Bản này mở tới Nguyên Anh; chưa có toàn bộ 12 cảnh giới trong lối chơi.
- Xác suất đột phá gốc theo mã nguồn tham khảo, một viên Trúc Cơ Đan tăng 15 điểm phần trăm. Thất bại mất tu vi đã tích của tầng cuối, tăng tâm ma và dưỡng thương; có điều dưỡng bằng linh thảo/linh thạch. Cơ chế thương tích là bản giản lược.
- Nhiệm vụ săn yêu thú 180 giây tự mô phỏng theo sức mạnh đệ tử; thắng nhận linh thạch, linh thảo, danh vọng; rút lui bị thương. Đây là nhiệm vụ đệ tử riêng, không phải điều khiển trực tiếp lính trên bản đồ.
- Thanh quy Cân bằng, Trọng Dụng Thiên Tài và Quảng Thu Môn Đồ; Biên niên giữ 120 sự kiện mới nhất.
- Tranh trường phái, chân dung đại diện và nền tiên phủ được lấy từ danh mục tranh có sẵn của dự án người dùng. Không sửa file gốc. Danh sách nguồn trong Reports/SectArtSources.json và ASSET_SOURCES.md.

## Cách chơi phần tông môn

1. Duy trì lương thực, xây Đệ Tử Xá và Diễn Võ Trường. Chạm Chánh Điện → Nâng cấp, trả 120 gỗ + 80 đá, chờ 60 giây mô phỏng.
2. Mở Xây dựng, sang các trang sau để dựng điện các. Phân công ngoại môn vào Linh Mỏ/Dược Viên/Luyện Đan Đường/Luyện Khí Điện.
3. Bấm Tông Môn / Đệ Tử ở góc trái. Tab Đệ Tử → chọn một trong năm trường phái để thu đồ: 30 thức ăn + 20 linh thạch mỗi người.
4. Tàng Kinh Các có người nghiên cứu và Tụ Linh Trận giúp tăng tốc tu luyện; Tụ Linh Trận hoạt động khi xây xong.
5. Khi đạt đỉnh cảnh giới và đầy tu vi, bấm Đột phá; có lựa chọn dùng hoặc không dùng đan và hiển thị xác suất/chi phí.
6. Xây Nhiệm Vụ Điện rồi cử đệ tử săn yêu thú. Đủ Tàng Kinh Các, 3 đệ tử, 30 danh vọng và vật tư để nâng Chánh Điện cấp 3.

Công thức xưởng thử nghiệm: 60 công-giây mỗi mẻ. Trúc Cơ Đan cần 15 linh thạch + 8 linh thảo; pháp khí cần 20 linh thạch + 12 đá. Pháp khí tăng 35 lực chiến nhiệm vụ. Đây là công thức giản lược của Unity, chưa phải hệ công thức Bảo Khố trên web.

## Cài và mở dự án

Android: dùng LangTroTan_Android_0.4.0.apk, Android 10 trở lên, hệ điều hành ARM64, OpenGL ES 3. Package com.langtrotan.prototype giữ nguyên để cập nhật. APK dùng khóa debug, chưa phát hành cửa hàng. Xóa ứng dụng/dữ liệu có thể mất save.

Unity Hub → Add project from disk → thư mục LangTroTan, Unity 6000.4.3f1 → Assets/Scenes/Village.unity → Play. Menu Làng Tro Tàn có Run checks and build Windows/Android. Android cần Android Build Support, SDK/NDK/OpenJDK. Windows thử nghiệm tại Builds/Windows_0.4/LangTroTan.exe; giữ các file/thư mục kèm theo.

Lưu tự động mỗi 30 giây thực, có nút Lưu. Chuyển save 0.3 thêm nền tông môn một lần và giữ làng cũ. Giao diện tông môn tạm dừng mô phỏng giống các cửa sổ quản lý hiện tại. Đóng game không tiếp tục mô phỏng.

## Đối chiếu với game web

Đã đọc các danh mục realms.js, sectBuildings.js, sectPolicies.js, sectTiers.js, recipes.js, studioAssetRegistry.js và formulas.js để tham khảo. Giữ quy tắc 9/4 tiểu cảnh, xác suất nền đột phá, một đan +15 điểm %, sản lượng nhân 1.2 theo cấp và hướng đánh đổi thanh quy. Còn chi phí, nhịp 6 phút/ngày, giới hạn dân, thăng hạng, nhiệm vụ, thương tích và chế tác là quy tắc prototype riêng để chơi nhanh.

Chưa tích hợp Node.js, MongoDB, tài khoản, thế giới 24/7, một giờ thật/ngày, công pháp trang bị, Ngũ Phong/ngân sách, chính trị, kế vị, gia phả, bốn châu, bí cảnh tranh đoạt hay lịch sử nhân quả. Không có thao tác lên database và không sửa/chạy dự án web gốc. Quân RTS bảo vệ Sơn Môn vẫn từ bản trước, chưa gắn từng lính với từng đệ tử tu luyện.

Ngày đêm, bốn mùa, mưa/tuyết, nguồn tài nguyên hữu hạn, ba làng AI, ngoại giao, săn bắn và trồng trọt của 0.3 vẫn có. AI làng chưa có hệ tu luyện, Ngũ Phong hoặc nội chính của game web.

## Kiểm chứng

105 kiểm tra logic/save chạy trước build, gồm 69 ca cũ và 36 ca tông môn mới. Có kiểm tra nâng/hủy và trả vật tư, trần cấp, dân không nhân đôi, lên tiểu cảnh/đột phá thất bại, xưởng thiếu nguyên liệu, nhiệm vụ và lưu tiến độ nâng cấp/đệ tử. Báo cáo trong Reports.

Ảnh giao diện kết xuất từ executable Windows với cảnh thử được chuẩn bị sẵn; cảnh có tài nguyên và công trình cấp cao để kiểm tra hình ảnh, không đại diện tiến độ mặc định khi chơi mới. Chưa đo hiệu năng hoặc kiểm tra thao tác trên điện thoại thật. Mục tiêu 30 FPS là cấu hình, chưa là kết quả đo. Chưa nghiệm thu toàn bộ MVP/GDD.
