# AI JAM

Turnaj robotov ovládaných umelou inteligenciou, postavený v Unity s ML-Agents.
1. Váš tím si z pripravených dielov postaví robota a domácu arénu.
2. Robota natrénujete pomocou učenia s posilňovaním (reinforcement learning).
3. V deň turnaja bojuje proti robotom ostatných tímov.

Priebeh podujatia:
- **Hackathon:** tímy stavajú a trénujú robota a arénu. Farby, ktoré vidí kamera robota, sa oznámia na začiatku.
- **Prezentácie a finálna validácia:** po prezentáciách organizátori skontrolujú každý tím.
- **Turnaj:** zápasy tímov.

## Obsah

| Kapitola | Čo v nej nájdete |
|---|---|
| [1. Inštalácia](#1-inštalácia) | Nástroje, kontrola inštalácie, otvorenie projektu |
| [2. Stavba](#2-stavba) | Robot, domáca aréna, tímový asset |
| [3. Tréning](#3-tréning) | Trénovacia scéna, spustenie trénera, použitie modelu |
| [4. Validácia](#4-validácia) | Kontrola pravidiel a testovací zápas |
| [5. Turnaj](#5-turnaj) | Turnajová scéna a ovládanie |
| [6. Pravidlá](#6-pravidlá) | Pravidlá tímu, robota, arény a zápasu |
| [7. Riešenie problémov](#7-riešenie-problémov) | Časté chyby a ich oprava |
| [8. Dokumenty](#8-dokumenty) | Podrobné dokumenty v projekte |

---

## 1. Inštalácia

Používajte presne tieto verzie. Projekt aj natrénované modely od nich závisia, preto nič neaktualizujte.

| Komponent | Verzia |
|---|---|
| Unity Editor | **6000.6.6f1** |
| Balík ML-Agents pre Unity (`com.unity.ml-agents`) | 4.1.0 (nainštaluje sa automaticky s projektom) |
| Python | **3.10.12** (`mlagents` vyžaduje 3.10.1–3.10.12) |
| Pythonový balík ML-Agents (`mlagents`) | **1.1.0** |
| PyTorch | **2.2.2** (verzia pre CUDA 12.1 na grafiky NVIDIA) |

### 1.1 Git
Nainštalujte [Git for Windows](https://git-scm.com/download/win).

### 1.2 Unity
1. Nainštalujte [Unity Hub](https://unity.com/download).
2. V Unity Hub otvorte **Installs → Install Editor → Archive** a nainštalujte **6000.6.6f1**. Ďalšie moduly pre platformy nepotrebujete.

### 1.3 Pythonové prostredie (Miniconda)
1. Nainštalujte [Minicondu](https://docs.conda.io/en/latest/miniconda.html).
2. Otvorte **Anaconda Prompt** a vytvorte prostredie `aijam`:

```
conda create -n aijam python=3.10.12 -y
conda activate aijam
```

3. Nainštalujte PyTorch. S grafikou NVIDIA použite verziu pre CUDA 12.1:

```
pip install torch==2.2.2 --index-url https://download.pytorch.org/whl/cu121
```

   Bez grafiky NVIDIA nainštalujte verziu pre procesor. Tréning je pomalší, ale funguje:

```
pip install torch==2.2.2
```

4. Nainštalujte ML-Agents a nechajte `setuptools` pod verziou 81. `mlagents` 1.1.0 ešte potrebuje `pkg_resources`, ktoré setuptools 81 odstránil.

```
pip install mlagents==1.1.0 "setuptools<81"
```

> **Chyby SSL?** Niektoré antivírusy (Norton, Avast…) a firemné siete kontrolujú HTTPS, takže `pip`, `conda` alebo `git` zlyhajú s chybou *certificate verify failed*. Riešenie:
> - **pip a conda:** exportujte balík certifikátov (CA bundle), ktorý obsahuje koreňový certifikát antivírusu, a nastavte ho cez `set PIP_CERT=<cesta-k-bundle.pem>` (pip) alebo `set CONDA_SSL_VERIFY=<cesta-k-bundle.pem>` (conda).
> - **git:** použite `git -c http.sslBackend=schannel clone ...`.

### 1.4 Kontrola inštalácie
V Anaconda Prompte s aktívnym prostredím `aijam`:

```
python --version
python -c "import torch; print(torch.__version__, torch.cuda.is_available())"
mlagents-learn --help
```

Mali by ste vidieť:
- `Python 3.10.12`;
- `2.2.2+cu121 True` (alebo `2.2.2 False` pri verzii pre procesor);
- nápovedu `mlagents-learn` bez chýb.

Ak `mlagents-learn` zlyhá s chybou `No module named 'pkg_resources'`, spustite `pip install "setuptools<81"`.

### 1.5 Stiahnutie a otvorenie projektu

1. Naklonujte repozitár:

```
git clone https://github.com/LuLa-90/AI-JAM-2025.git
```

2. V Unity Hub kliknite na **Add → Add project from disk** a vyberte priečinok `AI-JAM-2025`. Otvorte ho vo verzii **6000.6.6f1**. Prvý import trvá niekoľko minút.
3. **Importujte zdroje TextMesh Pro** (raz pre každý klon, priečinok nie je v Gite): **Window → TextMeshPro → Import TMP Essential Resources**. Bez nich skripty HUD hádžu chyby, Play mode sa pozastaví a tréning vyprší.
4. *(Voliteľné)* Načítajte rozloženie okien projektu: **Window → Layouts → Load Layout from File…** a vyberte `aijam_layout.wlt` v priečinku projektu.
5. Skontrolujte balíky: vo **Window → Package Manager** by mal mať *ML Agents* verziu **4.1.0**. V konzole (Console) by nemali byť červené chyby.

### 1.6 Prehľad projektu
| Kde | Na čo slúži |
|---|---|
| `Assets/Scenes/ConstructionScene` | Stavba robota |
| `Assets/Scenes/ArenaScene` | Stavba arény |
| `Assets/Scenes/TrainingScene` | Tréning a testovanie robota |
| `Assets/Validation/Validation.unity` | Kontrola robota a arény podľa pravidiel a testovací zápas |
| `Assets/Turnaj/Turnaj.unity` | Turnajová scéna (jeden zápas na jedno spustenie Play) |
| `Assets/Prefabs/Base Shapes` | Diely na stavbu (telá, ramená, kolesá, nárazníky, senzory) |
| `Assets/Prefabs/Robot Agent Template.prefab` | Robot, z ktorého začínate |
| `Assets/konfig.yaml` | Nastavenia trénera (hyperparametre PPO) |
| `Assets/Training Results` | Roboty a modely z minulého ročníka (dobrí súperi na tréning) |
| `Assets/Validation/Robots/ReferenceBot.prefab` | Robot, ktorý spĺňa všetky pravidlá. Použite ho ako vzor. |

---

## 2. Stavba

Táto kapitola ukazuje postup. Presné limity a zákazy sú v kapitole [6. Pravidlá](#6-pravidlá).

### 2.1 Robot

Otvorte **ConstructionScene**. Robot má tri podoby, každú vo vlastnej vrstve (layer):

| Podoba | Vrstva | Kto ju vidí | Pravidlá |
|---|---|---|---|
| Collidery + základný vzhľad | ConstructionView | fyzika, vy v editore | **Prísne**: pravidlá robota platia pre túto podobu |
| Efektný model | TournamentView | divák | **Voľné**: ľubovoľné modely, materiály a efekty, ale bez colliderov |
| Pohľad agenta | RobotView | kamera vášho robota | Iba farby z palety, nastavujú sa automaticky |

![Ten istý robot (ReferenceBot): vľavo collidery a základný vzhľad, vpravo pohľad agenta – biele časti a modrá batéria na čiernom pozadí](img/robot_views.png)
*Ten istý robot: vľavo collidery a základný vzhľad (ConstructionView), vpravo to, čo vidí kamera robota (RobotView).*

Postup:
1. **Začnite z `Robot Agent Template`** a ponechajte komponenty koreňa ([pravidlo 6.2.1](#621-koreň-robota)).
2. V **BehaviorParameters** nastavte **Behavior Name** na `RobotAgent`. Musí sa zhodovať s `konfig.yaml`.
3. **Pridajte diely** z `Assets/Prefabs/Base Shapes`: jedno telo, jedno rameno, dve sady kolies a najviac dva nárazníky ([pravidlo 6.2.2](#622-diely)).
   - Telo dajte priamo pod koreň. Všetko ostatné, vrátane kontajnerov `Wheels Left` a `Wheels Right`, presuňte pod telo.
   - Diely umiestnite a natočte podľa seba. Musia sa dotýkať, ale nesmú sa prekrývať.
4. **Pridajte senzory** (`ProximityRays`, `ButtSensorReciever`) podľa svojej stratégie. Kamera vidí steny čierne, takže ak má robot vnímať steny, pridajte `ProximityRays`.
5. **Farby pohľadu agenta** nenastavujte ručne. Robí to komponent `PartVisionColor` na každom diele ([pravidlo 6.2.5](#625-pohľad-agenta)).
6. Voliteľne pridajte efektný model vo vrstve **TournamentView**, bez colliderov.
7. Robota pretiahnite do okna Project a **uložte ho ako prefab** do priečinka svojho tímu.

![Editor s otvoreným prefabom ReferenceBot: hierarchia dielov, collidery v okne Scene a komponenty koreňa v Inspectore](img/editor_construction.png)
*Prefab `ReferenceBot` v Prefab Mode. V hierarchii vľavo je telo (`Body_Shape2`) priamo pod koreňom a kolesá aj rameno sú pod telom. V strede sú collidery, vpravo komponenty koreňa.*

### 2.2 Domáca aréna

Otvorte **ArenaScene**. Začnite z kópie `Assets/Turnaj/Prefabs/NeutralArena` (alebo `Assets/Prefabs/Environment (0, 0)`).

![Neutrálna aréna s dvoma robotmi na značkách Home a Guest](img/arena.png)
*Neutrálna aréna s robotmi na značkách Home a Guest.*

1. Podlahu a stenu (pevné collidery) nechajte bez zmeny.
2. Ponechajte značky `SpawnPoint` **Home** a **Guest** na ich miestach.
3. Pridajte svoje **pasce a bonusy**: vlastné skripty na trigger collideroch. Pre kameru robota ich označte červenou (pasca) alebo zelenou (bonus) vo vrstve RobotView.
4. Pridajte vzhľad, hudbu, zvuky, efekty alebo NPC podľa seba.
5. Voliteľne nastavte `ArenaViewAngle`: uhol kamery, z ktorého sa nápis na podlahe číta správne.
6. Arénu uložte ako **samostatný prefab**, nie vnútri prefabu robota.

Presné pravidlá arény: [6.3 Aréna](#63-aréna).

### 2.3 Tímový asset

V okne Project kliknite pravým tlačidlom → **Create → AI Jam → Turnaj Team**. Vyplňte:
- `TeamName`: názov tímu;
- `RobotPrefab`: váš robot;
- `ArenaPrefab`: vaša aréna.

<img src="img/inspector_team.png" width="480" alt="Inspector tímového assetu: Team Name, Robot Prefab a Arena Prefab">

Príklady sú v `Assets/Turnaj/Teams`.

---

## 3. Tréning

### 3.1 Príprava trénovacej scény
1. Otvorte **TrainingScene** a vyberte **AIJamManager**:
   - **Robot Prefab A:** váš robot. Tento sa učí.
   - **Robot Prefab B:** jeho súper. Môže to byť znova váš robot (obe kópie sa učia tú istú politiku) alebo niektorý robot z minulého ročníka z `Assets/Training Results`.
   - **Num Of Arenas:** One, Three alebo Five. Viac arén zbiera skúsenosti rýchlejšie, ale potrebuje výkonnejší počítač.

   <img src="img/inspector_aijammanager.png" width="480" alt="Inspector AIJamManager: Robot Prefab A, Robot Prefab B a Num Of Arenas">

   > **Pevný súper musí mať Inference Only.** Ak trénujete proti hotovému robotovi, otvorte jeho prefab a nastavte **BehaviorParameters → Behavior Type = Inference Only** s priradeným modelom. Inak ho tréner prevezme tiež (má rovnaký Behavior Name) a prestane hrať podľa svojho modelu.

2. Vyberte **Reward Editor** a nastavte koeficienty odmien (0–1): pohyb, natočenie k súperovi, uhol ramena, vzdialenosť od súpera, pozícia v aréne a kombinácia zrážok. Určujú, čo sa robot naučí.

   <img src="img/inspector_reward.png" width="480" alt="Inspector Reward Editor s posuvníkmi koeficientov odmien">
3. *(Voliteľné)* Upravte nastavenia PPO v `Assets/konfig.yaml`.

### 3.2 Spustenie trénera
V Anaconda Prompte:

```
conda activate aijam
cd <cesta-k>\AI-JAM-2025
mlagents-learn Assets/konfig.yaml --run-id=moj_prvy_beh --timeout-wait 300
```

Keď vypíše **„Listening on port 5004"**, stlačte v Unity **Play**. Roboty sa rozbehnú a tréner každých 5000 krokov vypíše štatistiky.

- **Zastavenie:** Ctrl+C v prompte alebo ukončenie Play mode. Model sa uloží.
- **Pokračovanie behu:** `mlagents-learn Assets/konfig.yaml --run-id=moj_prvy_beh --resume`
- **Beh odznova:** namiesto `--resume` pridajte `--force`.
- **Sledovanie priebehu:** v druhom prompte spustite `tensorboard --logdir results` a otvorte http://localhost:6006. Kumulatívna odmena (cumulative reward) by mala rásť.

> Editor sa veľmi spomalí, keď nie je aktívnym oknom. Počas tréningu ho majte vpredu.

### 3.3 Použitie natrénovaného modelu
1. Tréner uloží model do `results/<run-id>/RobotAgent.onnx` v priečinku projektu.
2. Skopírujte ho do `Assets/`, napríklad vedľa prefabu robota.
3. Otvorte prefab robota a pretiahnite `.onnx` do **BehaviorParameters → Model**.

   <img src="img/inspector_behavior.png" width="480" alt="Komponent Behavior Parameters: Behavior Name RobotAgent, Model a Behavior Type">

   *Behavior Name musí byť `RobotAgent`, Model je váš `.onnx` a Behavior Type je Default pri tréningu alebo Inference Only pri teste.*
4. Otestujte ho: v TrainingScene nastavte robotovi **Behavior Type** na **Inference Only** a stlačte Play *bez* spusteného trénera. Ak chcete trénovať ďalej, vráťte ho na **Default**.

---

## 4. Validácia

Validačná scéna kontroluje robota a arénu podľa [pravidiel](#6-pravidlá). Používajte ju priebežne už počas stavby, nie až na konci.

### 4.1 Kontrola pravidiel (Run validation)

1. Otvorte `Assets/Validation/Validation.unity`.
2. Vyberte objekt **Validation** a pridajte svoj tímový asset do zoznamu **Teams**.
3. V rozbaľovacom zozname vyberte svoj tím a stlačte **Play**.
4. Kliknite na **Run validation**. Dostanete zoznam chýb a varovaní; kliknutím na položku sa označí príslušný objekt.
   - **Chyby** musíte opraviť.
   - **Varovania** (napríklad vlastné skripty) posúdi človek.

Výsledok bez problémov vyzerá takto:

```
Validation – Test Alpha: PASS (0 errors, 0 warnings)
```

Kontrola modelu zlyhá, kým robota nenatrénujete ([kapitola 3](#3-tréning)). Všetko ostatné môže prejsť už počas stavby.

### 4.2 Testovací zápas (Run test match)

Kliknite na **Run test match**. Váš robot bojuje proti skriptovanému súperovi vo vašej aréne a meria sa šesť udalostí:
1. súper do vás narazí;
2. vypadnete z arény;
3. prejdete cez svoje pasce a bonusy;
4. prevrátite sa;
5. zásah do tela;
6. zásah do batérie.

Skontroluje sa aj to, že aréna nevytvára nič mimo objektu arény. Výsledky ukazujú, ako váš robot dostáva poškodenie. Nerozhodujú o tom, či prejdete.

Náhľad kamery ukazuje, čo váš robot vidí.

> **Tip:** Umiestnite kameru robota (`RobotCameraSensor`) tak, aby nevidela vlastného robota. Vlastné biele diely v obraze zakrývajú výhľad a robot si ich môže pomýliť so súperom, ktorého tiež vidí bielou farbou. V náhľade by mal byť iba súper, pasce, bonusy a čierne pozadie.

<img src="img/inspector_validation.png" width="480" alt="Inspector validačnej scény po validácii a testovacom zápase: tlačidlá, výsledky šiestich udalostí a náhľad kamery robota">

*Inspector validačnej scény po **Run validation** a **Run test match** (testovací tím, aréna bez pascí). Červený odtieň je farba Unity počas Play mode, nie chyba. Dole je náhľad kamery robota. Tlačidlo **Copy test report** skopíruje výsledky ako text.*

### 4.3 Finálna validácia

Organizátori po prezentáciách spúšťajú rovnaké kontroly pre každý tím. **Robot s chybami nemôže bojovať.** Varovania robota neblokujú, ale posúdi ich človek.

---

## 5. Turnaj

V turnajovej scéne si môžete zápas vyskúšať vopred.

1. Otvorte `Assets/Turnaj/Turnaj.unity` a vyberte objekt **Tournament**.
2. Pridajte svoj tímový asset do **Teams** a v rozbaľovacích zoznamoch vyberte **Team A** a **Team B** (napríklad váš tím proti testovaciemu).

   <img src="img/inspector_tournament.png" width="480" alt="Inspector Tournament Manager: výber tímov A a B, stav a zoznam tímov">
3. Stlačte **Play**, kliknite do okna **Game** (klávesy fungujú iba vtedy, keď je aktívne) a stlačte **medzerník** na spustenie odpočítavania.

![Turnajová scéna počas kola: časovač, skóre a panely HP oboch tímov](img/tournament.png)
*Okno Game počas kola: zostávajúci čas, skóre kôl a panely HP oboch tímov.*

| Kláves | Akcia |
|---|---|
| Medzerník | spustenie kola (3-2-1, FIGHT!!) |
| Num 8 / 5 / 2 | kamera zhora / spredu / zblízka |
| Num 1 / 3 | rotujúce kamery |
| Num 4 / 6 | sledovanie tímu A / tímu B |
| W ×2 | rozdeľovač (odtlačí roboty od seba) |
| Q ×2 / E ×2 | knockout tímu A / tímu B |
| R ×2 | opakovanie kola |
| T | pauza / pokračovanie |

Pravidlá zápasu (kolá, čas, body): [6.4 Zápas](#64-zápas). Celý návrh turnaja: [`Assets/Turnaj/DESIGN.md`](Assets/Turnaj/DESIGN.md).

---

## 6. Pravidlá

Robot a aréna musia tieto pravidlá splniť, aby tím mohol bojovať v turnaji. Kontroluje ich validačná scéna ([kapitola 4](#4-validácia)). Hodnoty označené *(nie je konečné)* sa oznámia na začiatku hackathonu.

Pravidlá kontrolujú **collidery**, nie vzhľad. Efektný model môže byť ľubovoľný, ale fyzický robot je rovnako veľký a silný ako roboty ostatných.

### 6.1 Tím
1. Tím je jeden asset `soTeam` s poľami `TeamName`, `RobotPrefab` a `ArenaPrefab`.
2. Robot a aréna musia byť **samostatné prefaby**.

### 6.2 Robot

#### 6.2.1 Koreň robota
1. Robot vychádza z prefabu **Robot Agent Template**.
2. Koreň si ponecháva `RobotAgent`, `RobotMovement`, `BehaviorParameters`, `Rigidbody`, `CollisionForwarder` a kamerový senzor. Mierka (scale) koreňa zostáva 1.
3. **Behavior Name** je `RobotAgent`. V `BehaviorParameters` musí byť priradený natrénovaný model (`.onnx`).

#### 6.2.2 Diely
1. Robot sa stavia iba z pripravených prefabov **Base Shapes**. Môžete ich ľubovoľne kombinovať v týchto limitoch:

   | Skupina | Prefaby | Počet |
   |---|---|---|
   | Telo | Body_Shape1, 2, 3 | presne 1 |
   | Rameno (zbraň) | Arm_Shape1, 2 | presne 1 |
   | Sady kolies | Wheels_Shape1, 2, 3 | presne 2: jedna pod `Wheels Left`, jedna pod `Wheels Right`. Rovnaký tvar dvakrát je povolený. |
   | Nárazníky | Bumper_Shape1, 2 | 0–2 |

2. **Štruktúra:**
   - Telo je **priamy potomok koreňa** robota.
   - **Každý iný diel je pod telom**, v ľubovoľnej hĺbke. Platí to pre rameno, nárazníky aj kontajnery `Wheels Left` / `Wheels Right` s ich wheel collidermi.
   - `RobotCameraSensor`, `ButtMarker` a `Bounding Box` môžu zostať pod koreňom.
3. **Diely sa musia dotýkať, ale nesmú sa prekrývať:**
   - kolesá, batéria, nárazníky a rameno sa dotýkajú tela;
   - zbraň sa dotýka ramena;
   - **diely sa nesmú schovávať do seba.** Koleso smie do tela zasahovať najviac **2 cm** a do nárazníka najviac **0,5 cm**. Koleso zapustené v tele je chyba.
4. **Uhly a pozície sú voľné.** Diely aj collidery v nich môžete ľubovoľne umiestniť a natočiť, pokiaľ platí bod 3.
5. Objekty so zónami poškodenia sa nesmú premenovať. Ich názvy určujú poškodenie a odolnosť: body, battery, wheels, armbase, weapon, bumper.

#### 6.2.3 Collidery
1. **Každý zapnutý collider na robote musí byť jeden z týchto:**
   - collider pripraveného dielu;
   - collider šablóny (wheel collidery, `ButtMarker`);
   - pevný collider senzora.

   Akýkoľvek iný collider je chyba. Nemôžete pridať štíty, hroty ani skryté bloky.
2. **Tvar:** collider si ponecháva pôvodný mesh. Mesh sa nesmie vymeniť za iný.
3. **Veľkosť:** každá os každého collidera sa smie líšiť od prefabu dielu najviac o **±25 %** *(nie je konečné)*. Každá os sa kontroluje zvlášť.
   - Zrkadlenie (−1 na osi) je povolené.
   - Veľkosť sa meria voči koreňu robota. Počíta sa aj zmena mierky nadradeného objektu.
4. **Pozícia:** voľná, obmedzená iba pravidlom o dotyku a prekrytí (6.2.2, bod 3). Kópia dielu pre pohľad agenta sa musí hýbať spolu s colliderom. Collidery senzorov a `ButtMarker` sú pevné.
5. Collidery zostávajú zapnuté, pevné (nie triggery) a ponechávajú si fyzikálny materiál.

#### 6.2.4 Uzamknutá fyzika
Tieto nastavenia musia zostať presne ako v šablóne alebo v prefabe dielu:
- hmotnosť, odpor (drag), uhlový odpor a ťažisko Rigidbody (koreň robota a rameno);
- 4 wheel collidery: polomer, odpruženie, trenie a pozícia;
- HingeJoint ramena: pružina, motor a limity.

Nepridávajte ďalšie Rigidbody ani Jointy.

#### 6.2.5 Pohľad agenta
1. Kamera robota vidí **iba vrstvu RobotView** na čiernom pozadí. Turnajová, trénovacia aj validačná scéna toto nastavenie vynútia.
2. Farby *(presné farby sa môžu zmeniť na začiatku hackathonu)*:

   | Farba | Význam |
   |---|---|
   | čierna | pozadie: podlaha, steny a všetko ostatné |
   | biela | časti robota |
   | červená | pasca (vyhni sa jej) |
   | zelená | bonus, buff (choď poň) |
   | modrá | batéria (útoč na ňu) |

3. **Žiadne falošné tvary.** Každý mesh vo vrstve RobotView na robote musí byť vlastná kópia dielu:
   - rovnaký mesh, pozícia, rotácia a veľkosť ako collider toho dielu;
   - kópia batérie je modrá, kópie ostatných dielov sú biele. Farby nastavuje komponent `PartVisionColor` na každom diele podľa `Assets/Validation/VisionPalette.asset`. Komponent aj jeho odkaz na paletu ponechajte;
   - žiadne ďalšie meshe vo vrstve RobotView.
4. To, kam kamera mieri, sa nekontroluje. Čo robot vidí, ukazuje náhľad vo validačnej scéne.

#### 6.2.6 Senzory
1. Senzory (`ProximityRays`, `ButtSensorReciever`) **nie sú obmedzené**. Agent má pevný počet vstupov, takže senzory navyše nedávajú výhodu.
2. Collidery senzorov sú pevné:
   - `ProximityRays/Sphere` zostáva presne ako v prefabe;
   - collidery `ButtSensorReciever` zostávajú vypnuté.
3. **Steny:** kamera vidí steny iba ako čierne. Na ich vnímanie slúži senzor `ProximityRays`.

#### 6.2.7 Skripty
Vlastné skripty na robote sú povolené, ale validácia ich vypíše ako varovania a organizátori ich skontrolujú. Skript, ktorý počas zápasu mení fyziku, HP alebo pravidlá, znamená diskvalifikáciu robota.

### 6.3 Aréna
1. **Stred v počiatku.** Koreň prefabu arény je v bode (0, 0, 0), bez rotácie, s mierkou 1. Podlaha je vo výške 0.
2. **Rovnaké pevné collidery ako neutrálna aréna** (`Assets/Turnaj/Prefabs/NeutralArena`). Každý collider, ktorý nie je trigger, musí zodpovedať neutrálnej aréne: rovnaký mesh, pozícia, rotácia a mierka. Podlaha a stena sú teda rovnaké pre všetkých. Nič nesmie chýbať ani pribudnúť.
3. **Žiadne pevné prekážky.** Každý iný collider musí byť **trigger**.
4. **Štartovacie značky.** Aréna má presne jednu značku `SpawnPoint` **Home** a jednu **Guest**. Sú pri podlahe a **súmerné podľa stredu**: pozícia Guest = −pozícia Home (v osiach x a z) a sú otočené o 180° voči sebe.
5. **Pasce a bonusy** sú skripty tímu na trigger collideroch. Validácia nevie posúdiť, čo vlastný kód robí, preto **vypíše každý vlastný skript** v aréne a posúdi ho človek.
6. **Pohľad agenta v aréne:**
   - červená alebo zelená farba smie byť iba na objekte, ktorý má trigger collider alebo je v ňom. Falošné pasce a bonusy sú chyba;
   - biela a modrá sú v aréne zakázané (falošní roboti a batérie);
   - nič iné do vrstvy RobotView nepatrí. Podlaha ani steny tam nemajú kópie; pozadie kamery je čierne.
7. **Povolené voľne:** vlastná hudba, zvuky, vizuálne a časticové efekty a NPC, ktoré nezasahujú do robotov. NPC nemajú pevné collidery (bod 3).
8. **Objekty vytvorené počas behu zostávajú pod arénou.** Všetko, čo aréna vytvorí (NPC, efekty, projektily, zvuky), musí byť **potomkom objektu arény**, aby sa medzi kolami zničilo spolu s ňou. Testovací zápas inak hlási chybu „Arena spawns" a turnajová scéna takéto objekty pri zmene kola zničí.
9. **Voliteľné:** `ArenaViewAngle` nastaví uhol kamery, z ktorého sa nápis na podlahe číta správne.

### 6.4 Zápas
1. **Kolá:** zápas sa hrá na 2 víťazné kolá.
   - 1. kolo je v domácej aréne tímu A.
   - 2. kolo je v domácej aréne tímu B.
   - 3. kolo sa hrá iba pri stave 1:1, v neutrálnej aréne.
2. **Štart:** každé kolo sa začína až na pokyn rozhodcu. Roboty štartujú na značkách Home (domáci tím) a Guest.
3. **Knockout:** kolo vyhráva robot, ktorého súper stratí HP.
4. **Pád a prevrátenie:** robot, ktorý vypadne z arény alebo sa prevráti, stratí časť HP na všetkých zónach a vráti sa na svoju značku.
5. **Čas:** kolo trvá 90 s. Po uplynutí času vyhráva robot s vyšším HP.
   - Rovnaké HP v 1. alebo 2. kole: kolo sa opakuje.
   - Rovnaké HP vo finálnom kole: **náhla smrť** (sudden death). Prehráva robot, ktorý ako prvý dostane poškodenie od súpera, vypadne alebo sa prevráti. Dotyk steny alebo podlahy sa nepočíta.
6. **Rozhodca** môže roboty od seba odtlačiť (rozdeľovač), kolo opakovať alebo robota vyradiť.
7. **Ligové body:**

   | Výsledok | Body |
   |---|---|
   | 2:0 | 3 |
   | 2:1 | 2 |
   | 1:2 | 1 |
   | 0:2 | 0 |

---

## 7. Riešenie problémov

| Problém | Riešenie |
|---|---|
| `mlagents-learn` vyprší skôr, než stlačíte Play | Pridajte `--timeout-wait 300` alebo stlačte Play skôr po „Listening on port 5004". |
| Play mode sa hneď pozastaví s chybou TMP `NullReferenceException` | Importujte TMP Essential Resources (časť 1.5, krok 3). |
| V TrainingScene sa neobjavia roboty | Nastavte Robot Prefab A a B na **AIJamManager** (časť 3.1). |
| `No module named 'pkg_resources'` | `pip install "setuptools<81"` |
| `torch.cuda.is_available()` vracia `False` s grafikou NVIDIA | Nainštalujte verziu cu121 (časť 1.3) a aktualizujte ovládač grafiky. |
| Súper sa počas tréningu správa čudne | Nastavte mu Behavior Type na **Inference Only** (časť 3.1). |
| Chcete, aby model robota prestal konať | Odstráňte komponent `DecisionRequester`. Vypnutie nestačí: ML-Agents aj tak žiada rozhodnutia. |
| Chyby certifikátov v pip, conda alebo git | Pozrite poznámku o SSL v časti 1.3. |

## 8. Dokumenty

Dokumenty v `Assets/` sú v angličtine. Pravidlá v kapitole 6 z nich vychádzajú.

| Dokument | Obsah |
|---|---|
| [`Assets/Validation/ROBOT_RULES.md`](Assets/Validation/ROBOT_RULES.md) | Pravidlá robota pre tímy (anglicky) |
| [`Assets/Validation/ARENAS.md`](Assets/Validation/ARENAS.md) | Pravidlá arény, farby pohľadu robota, vrstvy |
| [`Assets/Validation/ROBOT_PARTS.md`](Assets/Validation/ROBOT_PARTS.md) | Všetky diely, ich collidery a tabuľka poškodení |
| [`Assets/Validation/DESIGN.md`](Assets/Validation/DESIGN.md) | Ako funguje validácia a testovací zápas |
| [`Assets/Turnaj/DESIGN.md`](Assets/Turnaj/DESIGN.md) | Ako funguje turnajová scéna |
| [`documentation.md`](documentation.md) | Pôvodná dokumentácia projektu |
| [`Config_README.md`](Config_README.md) | Pôvodné poznámky k tréningu (konfigurácia je teraz `Assets/konfig.yaml`) |
