# Flex Tuning Tray

## 설명

갤럭시북 플렉스 모델의 태블릿 모드에서의 전력제한을 토글하기 편하게 해주는 트레이 유틸리티.

> https://gall.dcinside.com/mgallery/board/view/?id=laptop&no=1222830

위 글 참고하여 제작함.

갤럭시 북 플렉스 2에서 확인 결과  태블릿 모드 전환 시 PL1 8W로 감소.


반대로 esifsvc 서비스 정지 후에는 20W 유지하는 것을 확인.

이 유틸리티는 작업표시줄 아래에서 그걸 키고 끄기 편하게 해 줌. 상태 확인도 해 주고.

다만 서비스 재시작 후 다시 8W 제한을 걸지는 않는 것으로 보임. 추가 작업 요망.

## 사용방법

설치는 원하는 위치에. Release의 압축파일을 풀어서 놓기만 하면 됨.

`%LOCALAPPDATA%\Programs\FlexTuningTray` 경로를 개인적으로 권장. (굳이 시스템와이드로 설치할 건 아니잖아?)

못 믿겠으면 소스코드 확인하고 직접 빌드해도 OK. 클론 (모르겠음 옆에 zip으로 이거 받고) 한 다음 폴더에서

```
dotnet build --configuration Release
```

하면 bin... 경로에 있는 거 그대로 복붙.



FlexTuningTray.exe를 백그라운드로 실행하면 아래 트레이 생김. 버튼들은 자명함.

같이 제공한 Register.csx를

`dotnet run register.csx ./FlexTuningTray.exe enable`

이렇게 하면 시작프로그램으로 등록 가능.

---

귀찮아서, 썩 좋아하지는 않지만 AI 보조 받아 구현함.

당연하지만 정상 작동을 보장하지는 않음. 오작동에 대해서는 책임 없음...

---

근데 이 노트북 나온지도 오래된 건데 이걸 누가 쓰긴 할까?

---

트레이 아이콘의 원 저작자는 삼성전자. 저작권 문제가 있을 경우 교체하겠음.