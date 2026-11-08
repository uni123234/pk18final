# Практичне заняття 18: Фіналізація проєкту

**Дисципліна:** Об'єктно-орієнтоване програмування (3 курс)  
**Тема:** Фіналізація та повна інтеграція навчального проєкту: командний інтерфейс (CLI Facade), бізнес-логіка покрокового бою (BLL), підсистема постійності та кешування портретів (Persistence & Cache-Aside), нативний графічний інтерфейс (WPF MVVM), та пакування для розгортання (Single-File SCD/FDD)  
**Стек / Платформа:** .NET 8 Windows (WPF Native Desktop, C# 12), xUnit 2.5, MSBuild  

---

## 🎯 Мета роботи
- Завершити повний цикл проєктування та розробки платформи рольової гри та каталогу персонажів згідно з принципами Clean Architecture та SOLID.
- **Командний інтерфейс (CLI):**
  - Централізований фасад `CliFacade` з розбором аргументів (`ArgsParser`) та консольною графікою `CLIGraphic`.
  - Три стратегії команд: вивід інформації (`InformationStrategy`), зміна стану та покрокові дуелі (`TransformativeStrategy`), інфраструктура й збереження (`InfrastructureStrategy`).
- **Бізнес-логіка (BLL):**
  - Детерміновані математичні моделі фізичного та магічного бою з урахуванням броні, опору та пробиття:
    $$dmg_{phys} = \max(0, dmg_{flat} - armor_{tgt}) \times \max(0, 1 - \max(0, res_{tgt} - res_{pen}))$$
    $$dmg_{magic} = \left( dmg_{flat} \times \max(0, 1 - res_{tgt} + res_{pen}) + bonus_{flat} \right) \times \max(0.1, 1 + bonus_{prof} - debuff_{ability})$$
  - Покроковий рушій бою (`TurnBasedBattleEngine`) з чергою ходів за показником швидкості (`Speed`), бонусним слотом ходу при перевазі $\ge 1.5\times$, та протоколюванням у `BattleHistory`.
  - Підсистема статус-ефектів (`Buff`, `Debuff`, `DoT`, `Stun`).
  - Парсинг та підготовка даних віддаленого REST API Genshin Impact (`https://genshin.jmp.blue/`).
- **Постійність даних та буферизація (Persistence & Cache-Aside):**
  - Локальний дисковий буфер портретів (`LocalFileAssetCacheService`) у `cache/portraits/` з підтримкою офлайн-генерації та телеметрії (Hits, Misses, Hit Ratio %).
  - Система збереження в окремі слоти (`GameSaveManager`) з атомарним записом (`saves/{slot}.save.json`) та автоматичним продовженням з найновішого сейву (`continue`).
- **Графічний інтерфейс (WPF Desktop Native GUI):**
  - Нативне вікно Windows Presentation Foundation з темною темою.
  - Дві функціональні вкладки:
    1. **Character Catalog:** прокручуваний список композитних карток персонажів із портретами, зірочками рідкісності, індикатором стихії та прогрес-баром HP.
    2. **Text Game Console:** стилізований термінал із живим виводом логів дуелей, кнопками вибору/завантаження сейвів, запуску дуелі та полем командного рядка.
  - Архітектурний патерн **MVVM** (`MainViewModel`, `CharacterCardViewModel`, `RelayCommand`, адаптер `GuiDisplayer`).
- **Розгортання та пакування (Deployment):**
  - Оптимізована Release-збірка, профілі публікації Self-Contained (`win-x64-standalone.pubxml`) та Framework-Dependent (`win-x64-portable.pubxml`).
  - Автоматизовані скрипти: інсталятор `install.bat`, лаунчери `run.bat` (GUI), `run_cli.bat` (CLI), деінсталятор `uninstall.bat`.
- **Дизайн-документ та верифікація:**
  - Оформлення повного архітектурного дизайн-документа ([DESIGN.md](file:///d:/Project/lerning/pk18final/DESIGN.md)).
  - Покриття 16 модульними та інтеграційними тестовими сьютами xUnit.

---

## 🏛️ Архітектура рішення

```
pk18final/
├── GUI/                                # Шар графічного інтерфейсу (WPF MVVM)
│   ├── App.xaml / App.xaml.cs          # Застосунок WPF та ресурси темної теми
│   ├── Views/MainWindow.xaml           # Головне вікно (Каталог карток + Термінал гри)
│   ├── ViewModels/MainViewModel.cs     # Головна ViewModel вікна
│   ├── ViewModels/CharacterCardViewModel.cs # ViewModel композитної картки персонажа
│   ├── Common/RelayCommand.cs          # Реалізація ICommand для прив'язки дій
│   └── Services/GuiDisplayer.cs        # Адаптер відображення доменних повідомлень у GUI
├── Core/                               # Доменна та бізнес-логіка (BLL)
│   ├── Characters/
│   │   ├── Combat/CombatCalculator.cs  # Математичні формули фізичної та магічної шкоди
│   │   ├── TurnBased/                  # Покроковий рушій дуелей, таймлайн та аудит
│   │   ├── Models/                     # Character, Item, Ability, StatusEffect, GameConfig
│   │   └── Services/Game.cs            # Доменний фасад застосунку
│   ├── FileWorkspace/                  # Стейт-машина файлового простору
│   └── Text/                           # Ієрархія документообігу та шаблонів
├── CLI/                                # Рівень консольного представлення
│   ├── Facades/CliFacade.cs            # Фасад взаємодії з консоллю
│   ├── Strategies/                     # Стратегії команд (Information, Transformative, Infra)
│   └── Rendering/                      # IRenderer та CLIGraphic (таблиці, прогрес-бари)
├── Infra/                              # Рівень інфраструктури
│   ├── Caching/                        # Дисковий буфер портретів (Cache-Aside)
│   ├── Data/SaveSystem/                # Сейв-менеджер слотів та продовження з останнього сейву
│   └── Remote/                         # HTTP-клієнт Genshin API з офлайн-фолбеком
├── Properties/PublishProfiles/         # Профілі публікації (Self-Contained та Portable)
├── deploy/                             # Пакунок розгортання (конфігурація, маніфест, гайд)
├── Tests/                              # 16 тестових сьютів xUnit
├── install.bat / uninstall.bat         # Скрипти встановлення та видалення
├── run.bat / run_cli.bat               # Одноклавішні лаунчери GUI та CLI
├── build.bat / publish.bat             # Скрипти автоматизованої збірки та пакування
├── Program.cs                          # Точка входу з перемиканням режимів (GUI / CLI)
├── Project.csproj                      # Метадані збірки, версія 1.0.0, оптимізації Release
├── Project.sln                         # Файл рішення
├── DESIGN.md                           # Архітектурна специфікація та SDD проєкту
├── README.md                           # Документація фінального проєкту
└── submission.txt                      # Посилання на репозиторій та DESIGN.md
```

---

## 🧪 Верифікація та тестування (xUnit)

Рішення покрито 16 модульними тестовими наборами:
1. `BusinessLogicTests.cs`: математичні формули фізичної/магічної шкоди, пробиття опору, черга ходів за швидкістю, статус-ефекти, журнал ходів.
2. `PersistenceTests.cs`: Cache-Aside влучання/промахи, офлайн-фолбек зображень, атомарний запис слотів, продовження з найновішого сейву.
3. `GuiTests.cs`: MVVM прив'язки, `RelayCommand`, генерація карток персонажів, запуск дуелей та логи в GUI.
4. `DeploymentTests.cs`: профілі публікації SCD/FDD, маніфест розгортання, файли конфігурації, скрипти запуску.
5. `CliFacadeTests.cs` & `CliCommandTests.cs`: токенізація аргументів, лапки, таблиці `CLIGraphic`, стратегії.
6. `BehavioralPatternsTests.cs`: патерни Mediator, Memento, Observer, Chain of Responsibility.
7. `StatePatternTests.cs`: скінченний автомат станів робочого простору.
8. `RemoteApiTests.cs` & `ApiCliCommandTests.cs`: HTTP REST-клієнт та мапінг DTO.
9. `TreeGenerativePatternTests.cs` & `TextTests.cs`: Composite, Prototype, Builder та Document Tree.
10. `CharactersTests.cs`, `InfraTests.cs`, `ScenarioIntegrationTests.cs`, `MoqDemonstrationTests.cs`: інтеграційне та регресійне покриття.

---

## 🚀 Інструкція запуску та використання

### Швидкий запуск:
- **Встановлення (створення ярлика на робочому столі):** запустити `install.bat`.
- **Запуск графічного інтерфейсу (WPF GUI):** подвійний клік по `run.bat` або створеному ярлику.
- **Запуск консольного інтерфейсу (CLI):** подвійний клік по `run_cli.bat`.

### Команди CLI (у терміналі або через поле `CLI >` у GUI):
```bash
# Каталог персонажів
chars                               # Таблиця персонажів із мітками кешування
char Jean                           # Детальна картка персонажа
api-import raiden                   # Імпорт персонажа з Genshin API та буферизація портрета

# Покроковий бій та ігровий цикл
duel Jean Diluc                     # Проведення покрокової дуелі з таблицею ходів
history                             # Таблиця ходів останнього бою

# Постійність даних та кеш
save my_adventure                   # Зберегти гру в іменований слот
saves                               # Список усіх збережених слотів
continue                            # Продовжити сесію з найновішого сейву
cache status                        # Переглянути статистику дискового кешу
cache prefetch                      # Завантажити портрети для всіх персонажів
```
