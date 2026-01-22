import cv2
import mediapipe as mp
import socket
import math
import time

# === 配置 ===
UDP_IP = "127.0.0.1"
UDP_PORT = 5052

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
mp_hands = mp.solutions.hands
mp_draw = mp.solutions.drawing_utils
hands = mp_hands.Hands(
    max_num_hands=1, 
    min_detection_confidence=0.7,
    min_tracking_confidence=0.5
)

cap = cv2.VideoCapture(0)

# 状态防抖
last_fist_time = 0
fist_cooldown = 1.0 # 握拳切换冷却时间(秒)

print("✨ 手势控制 v2.0 已启动！")
print("👉 张开手掌移动 -> 控制旋转/平移")
print("✊ 握紧拳头 -> 切换模式 (旋转 <-> 平移)")

while cap.isOpened():
    success, img = cap.read()
    if not success: continue

    img = cv2.flip(img, 1) # 镜像翻转
    h, w, c = img.shape
    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    results = hands.process(img_rgb)

    if results.multi_hand_landmarks:
        for lm in results.multi_hand_landmarks:
            mp_draw.draw_landmarks(img, lm, mp_hands.HAND_CONNECTIONS)
            
            # 1. 获取手掌中心 (X, Y)
            # 0号点是手腕，9号点是中指根部
            wrist = lm.landmark[0]
            mid_base = lm.landmark[9]
            
            palm_x = (wrist.x + mid_base.x) / 2
            palm_y = (wrist.y + mid_base.y) / 2
            
            # 2. 计算捏合距离 (用于缩放)
            t = lm.landmark[4] # 拇指
            i = lm.landmark[8] # 食指
            dist = math.sqrt((t.x - i.x)**2 + (t.y - i.y)**2)
            
            # 3. ✊ 简单的握拳检测
            # 逻辑：检查 食指(8)、中指(12)、无名指(16)、小指(20) 的指尖是否都在各自指根下方
            # 注意：在图像坐标系中，y越大越靠下
            # 获取指尖和指关节
            fingers_folded = 0
            # 检查食指到小指 (索引 8,12,16,20) vs (索引 6,10,14,18)
            if lm.landmark[8].y > lm.landmark[6].y: fingers_folded += 1
            if lm.landmark[12].y > lm.landmark[10].y: fingers_folded += 1
            if lm.landmark[16].y > lm.landmark[14].y: fingers_folded += 1
            if lm.landmark[20].y > lm.landmark[18].y: fingers_folded += 1
            
            is_fist = 1 if fingers_folded >= 3 else 0 # 3根手指折叠就算握拳
            
            # 4. 发送数据给 Unity
            # 格式: "X, Y, 捏合距离, 是否握拳"
            # 例如: "0.5, 0.4, 0.2, 0"
            msg = f"{palm_x:.3f},{palm_y:.3f},{dist:.3f},{is_fist}"
            sock.sendto(msg.encode(), (UDP_IP, UDP_PORT))
            
            # 画面提示
            status_text = "FIST" if is_fist else "PALM"
            color = (0, 0, 255) if is_fist else (0, 255, 0)
            cv2.putText(img, f"Pos:({palm_x:.2f}, {palm_y:.2f}) {status_text}", (10, 30), 
                        cv2.FONT_HERSHEY_SIMPLEX, 0.7, color, 2)

    cv2.imshow("Hand Control v2", img)
    if cv2.waitKey(5) & 0xFF == 27: break

cap.release()
cv2.destroyAllWindows()
