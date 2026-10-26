# Tu Tiên Ký — bản chuyển toàn bộ sang Unity 3D đang phát triển

Bản đang làm nằm tại `Builds/Windows_FullPort_Dev/LangTroTan.exe`. Chạy cùng các thư mục đi kèm. Chưa xuất APK mới; cổng xuất Android vẫn khóa cho đến khi hoàn tất và kiểm chứng toàn bộ yêu cầu.

## Cách chơi thử hiện tại

- Dự án Unity: mở thư mục này bằng Unity 6000.4.3f1, mở `Assets/Scenes/Village.unity`, nhấn Play.
- Máy chủ phát triển hiện tại: `http://127.0.0.1:8899`, lượt thử `unity-port-qa`. Có thể dùng nút Kết nối để mở lượt đã lưu hoặc khai sơn một lượt mới.
- Máy chủ dùng bản luật chơi tham khảo tại `C:/Users/Admin/Documents/Codex/tu-tien-port/backend`, cùng cơ sở dữ liệu thử riêng `tu_tien_unity_port_qa` trên cổng 27028. Máy chủ cần chạy để chơi; địa chỉ localhost chỉ dùng trên cùng máy tính.
- Sơn Môn hiển thị khu xây dựng 3D. Kéo để di chuyển, cuộn chuột hoặc chụm hai ngón để thu phóng. Chạm công trình hoặc cây/đá/hươu để quản lý.
- Xây dựng Sơn Môn mở 11 loại công trình sinh hoạt, sản xuất và tường/cổng. Chọn loại, chạm đất, xoay rồi xác nhận. Máy chủ kiểm tra vùng xây, va chạm, tài nguyên và lối đi.
- Nhà phục dịch nâng riêng tới cấp 3; cần người rảnh và vật tư. Hủy nâng hoàn 80% vật tư trong giới hạn kho. Chánh Điện và các điện gốc dùng công thức linh thạch của game tham khảo.
- Cử người khai thác cây, đá và săn hươu: người đi đến nguồn, làm việc rồi mang vật tư về. Nguồn có giới hạn; cây và hươu có hồi phục. Ruộng cần người làm, chịu độ ẩm và mùa, có lúa/rau và gieo tự động khi đủ giống.
- Đại Điện có 14 nhóm nội dung, Quyết Nghị có 69 biểu mẫu thao tác từ mã nguồn gốc. Việc mở được biểu mẫu chưa đồng nghĩa mọi tình huống đã được kiểm chứng đầy đủ.

## Phạm vi kiểm tra

Báo cáo nằm tại `Reports/FullPort/`. Luật thời gian dùng một giờ thực cho một ngày, bốn ngày một mùa; bốn mùa lặp không giới hạn. Máy chủ lưu tiến trình và xử lý yêu cầu gửi lại bằng mã thao tác để tránh trừ tài nguyên hai lần.

Còn phải hoàn thiện và kiểm chứng: toàn bộ tương tác nâng cao của game tham khảo, quân đội và chiến tranh trên bản đồ 3D, chuyển tiến trình Unity cũ, hiệu năng và thao tác trên điện thoại. Đây chưa phải bản hoàn chỉnh để phát hành.

Các APK và hướng dẫn 0.4 là bản thử cũ, không chứa chế độ chuyển toàn bộ hiện tại. Xem `README_0.4.md` khi cần đối chiếu bản cũ.
