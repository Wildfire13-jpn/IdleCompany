using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameBootstrapper : Node
{
	// Цветовая палитра
	private readonly Color COLOR_BG = Color.FromHtml("#11141a");
	private readonly Color COLOR_PANEL = Color.FromHtml("#1e222b");
	private readonly Color COLOR_TEXT = Color.FromHtml("#e2e8f0");
	private readonly Color COLOR_TEXT_MUTED = Color.FromHtml("#94a3b8");
	private readonly Color COLOR_PROGRESS = Color.FromHtml("#3b82f6");
	private readonly Color COLOR_PROGRESS_BG = Color.FromHtml("#0f172a");
	private readonly Color COLOR_ACCENT = Color.FromHtml("#10b981");
	private readonly Color COLOR_CONTRACT = Color.FromHtml("#fbbf24");
	private readonly Color COLOR_LOCKED = Color.FromHtml("#4a5568");

	// --- ДАННЫЕ ИГРЫ ---
	public class SkillUpgradeData
	{
		public string Title;
		public string Description;
		public double BonusPercent;
		public double Cost;
		public int RequiredLevel;
		public string Type;
		public string Rarity;
		public bool IsBought = false;
		public Button BuyBtn;
	}

	public class SkillData
	{
		public string Title;
		public string IconName = "🔷";
		public int Level = 1;
		public double BaseIncome;
		public double CurrentProgress = 0;
		public double Speed;
		public bool IsRunning = true;
		public bool IsUnlocked = false;

		public Label LevelLabel;
		public Label IncomeLabel;
		public ProgressBar UIProgressBar;
		public Button PauseBtn;
		public PanelContainer CardPanel;
		public List<SkillUpgradeData> SkillUpgrades = new List<SkillUpgradeData>();

		public double GetCurrentIncome() => BaseIncome * Level;
		public double GetUpgradeCost() => Math.Round(BaseIncome * 5 * Math.Pow(1.2, Level - 1));
		public double GetMaxProgress() => 100 + (Level * 15);
	}

	public class GlobalUpgradeData
	{
		public string Title;
		public string Description;
		public double BaseCost;
		public double CostMultiplier = 1.15;
		public int Level = 0;
		public int MaxLevel = 20;
		public double IncomeBonus;
		public double SpeedBonus;
		public double XPBonus;

		public Label LevelLabel;
		public Label EffectLabel;
		public Button UpgradeBtn;

		public double GetCost() => Math.Round(BaseCost * Math.Pow(CostMultiplier, Level));
		public string GetEffectText() 
		{
			if (IncomeBonus > 0) return $"+{(IncomeBonus * Level * 100):F0}% Income";
			if (SpeedBonus > 0) return $"+{(SpeedBonus * Level * 100):F0}% Speed";
			if (XPBonus > 0) return $"+{(XPBonus * Level * 100):F0}% XP";
			return "";
		}
	}

		// --- ПОЛЯ ИГРЫ ---
	private List<SkillData> _skills = new List<SkillData>();
	private List<GlobalUpgradeData> _companyUpgrades = new List<GlobalUpgradeData>();
	private List<GlobalUpgradeData> _rdUpgrades = new List<GlobalUpgradeData>();
	
	private double _globalIncomeMultiplier = 1.0;
	private double _globalSpeedMultiplier = 1.0;
	private double _globalXPMultiplier = 1.0;
	private double _totalBalance = 0;
	private SkillData _selectedSkill;
	private int _companyLevel = 1;
	private string _companyName = "My Startup";
	
	private double _gameSpeedMultiplier = 1.0; 
	private double _boostRemainingTime = 0; 
	private double _boostCooldownRemaining = 0; 
	private Button _boostButton;
	private Label _boostLabel;

	// UI Элементы
	private Label _balanceLabel;
	private Label _runningSkillLabel;
	private Label _companyLevelLabel;
	private Label _centerSkillLevel;
	private ProgressBar _centerProgressBar;
	private Button _centerPauseBtn;
	private Button _centerUpgradeBtn;
	private VBoxContainer _skillTabContainer;
	private List<Button> _buyModeButtons = new List<Button>();

	// Контракты
	private double _contractProgress = 0;
	private double _contractMax = 10000;
	private Label _contractLabel;
	private ProgressBar _contractBar;
	private double _contractReward = 500;

	// Массовые покупки
	private enum BuyMode { One, Ten, Hundred, Max }
	private BuyMode _currentBuyMode = BuyMode.One;

	private MenuSystem _menuSystem;
	private Button _menuToggleButton;
	private CanvasLayer _canvasLayer;

	private string _savePath = "user://savegame.json";

	public override void _Ready()
	{
		// --- ИГРОВЫЕ ДАННЫЕ ---
		InitializeSkill("Coding", "</>", 60, 10, 0);
		InitializeSkill("Design", "🎨", 40, 35, 3);
		InitializeSkill("Marketing", "📢", 25, 120, 5);
		InitializeSkill("Sales", "💼", 15, 250, 8);
		InitializeSkill("Support", "🎧", 30, 80, 12);
		InitializeSkill("Management", "👔", 10, 600, 15);

		// Добавляем апгрейды для всех скиллов (СОКРАЩЕНО ДЛЯ ПРИМЕРА)
		var coding = _skills[0];
		coding.SkillUpgrades.Add(new SkillUpgradeData { Title = "Hotkey Mastery", Description = "+12% Speed", BonusPercent = 0.12, Cost = 1000, RequiredLevel = 2, Type = "Speed", Rarity = "Normal" });
		coding.SkillUpgrades.Add(new SkillUpgradeData { Title = "Linter Pipeline", Description = "+18% Speed", BonusPercent = 0.18, Cost = 3000, RequiredLevel = 5, Type = "Speed", Rarity = "Normal" });
		coding.SkillUpgrades.Add(new SkillUpgradeData { Title = "Hot Reload", Description = "+25% Speed", BonusPercent = 0.25, Cost = 8000, RequiredLevel = 10, Type = "Speed", Rarity = "Normal" });
		coding.SkillUpgrades.Add(new SkillUpgradeData { Title = "CI/CD Pipeline", Description = "+8% Speed (All)", BonusPercent = 0.08, Cost = 20000, RequiredLevel = 15, Type = "Speed", Rarity = "Milestone" });
		coding.SkillUpgrades.Add(new SkillUpgradeData { Title = "Code Generator", Description = "+35% Speed", BonusPercent = 0.35, Cost = 50000, RequiredLevel = 20, Type = "Speed", Rarity = "Normal" });
		coding.SkillUpgrades.Add(new SkillUpgradeData { Title = "Build Farm", Description = "+14% Speed (All)", BonusPercent = 0.14, Cost = 120000, RequiredLevel = 30, Type = "Speed", Rarity = "Milestone" });
		coding.SkillUpgrades.Add(new SkillUpgradeData { Title = "Compiler Cache", Description = "+45% Speed", BonusPercent = 0.45, Cost = 300000, RequiredLevel = 40, Type = "Speed", Rarity = "Normal" });
		coding.SkillUpgrades.Add(new SkillUpgradeData { Title = "Quantum Compiler", Description = "+22% Speed (All)", BonusPercent = 0.22, Cost = 800000, RequiredLevel = 55, Type = "Speed", Rarity = "Milestone" });
		coding.SkillUpgrades.Add(new SkillUpgradeData { Title = "Singularity Core", Description = "+35% Speed (All)", BonusPercent = 0.35, Cost = 2500000, RequiredLevel = 75, Type = "Speed", Rarity = "Capstone" });

		_companyUpgrades.Add(new GlobalUpgradeData { Title = "Better Laptop", Description = "+10% XP each", BaseCost = 500, XPBonus = 0.10, MaxLevel = 20 });
		_companyUpgrades.Add(new GlobalUpgradeData { Title = "CRM Software", Description = "+10% Money each", BaseCost = 1500, IncomeBonus = 0.10, MaxLevel = 20 });
		_companyUpgrades.Add(new GlobalUpgradeData { Title = "Office Espresso Machine", Description = "+10% Speed each", BaseCost = 10000, SpeedBonus = 0.10, MaxLevel = 20 });

		_rdUpgrades.Add(new GlobalUpgradeData { Title = "Process Automation", Description = "+4% Money each", BaseCost = 15000000, IncomeBonus = 0.04, MaxLevel = 50 });

		LoadGame();
		CheckUnlockTiers();

		_selectedSkill = _skills[0];

		BuildUI();
		GenerateNewContract();
		UpdateUI();
	}

		private void InitializeSkill(string title, string icon, double speed, double income, int unlockLevel)
	{
		var skill = new SkillData
		{
			Title = title,
			IconName = icon,
			Speed = speed,
			BaseIncome = income,
			IsUnlocked = _companyLevel >= unlockLevel
		};
		_skills.Add(skill);
	}

	// --- СОХРАНЕНИЕ / ЗАГРУЗКА ---
	public void SaveGame()
	{
		var data = new Godot.Collections.Dictionary
		{
			["balance"] = _totalBalance,
			["company_level"] = _companyLevel,
			["company_name"] = _companyName
		};

		var skillLevels = new Godot.Collections.Array();
		foreach (var s in _skills) skillLevels.Add(s.Level);
		data["skill_levels"] = skillLevels;

		var globalLevels = new Godot.Collections.Array();
		foreach (var u in _companyUpgrades.Concat(_rdUpgrades)) globalLevels.Add(u.Level);
		data["global_levels"] = globalLevels;

		string json = Json.Stringify(data);
		using var file = FileAccess.Open(_savePath, FileAccess.ModeFlags.Write);
		file.StoreString(json);
	}

	public void LoadGame()
	{
		if (!FileAccess.FileExists(_savePath)) return;
		using var file = FileAccess.Open(_savePath, FileAccess.ModeFlags.Read);
		string json = file.GetAsText();
		var data = Json.ParseString(json).AsGodotDictionary();

		_totalBalance = (double)data["balance"];
		_companyLevel = (int)(double)data["company_level"];
		_companyName = (string)data["company_name"];

		var skillLevels = (Godot.Collections.Array)data["skill_levels"];
		for (int i = 0; i < _skills.Count && i < skillLevels.Count; i++)
			_skills[i].Level = (int)(double)skillLevels[i];

		var globalLevels = (Godot.Collections.Array)data["global_levels"];
		int idx = 0;
		foreach (var u in _companyUpgrades.Concat(_rdUpgrades))
		{
			if (idx < globalLevels.Count)
				u.Level = (int)(double)globalLevels[idx++];
		}
		RecalculateModifiers();
	}

	// --- СИСТЕМА РАЗБЛОКИРОВКИ ---
	private void CheckUnlockTiers()
	{
		int[] unlockLevels = { 0, 3, 5, 8, 12, 15 };
		for (int i = 0; i < _skills.Count; i++)
		{
			bool unlocked = _companyLevel >= unlockLevels[i];
			_skills[i].IsUnlocked = unlocked;
			if (!unlocked) _skills[i].IsRunning = false;
		}
	}

	// --- ПЕРЕСЧЕТ МНОЖИТЕЛЕЙ ---
	private void RecalculateModifiers()
	{
		double totalIncome = 0;
		double totalSpeed = 0;
		double totalXP = 0;

		foreach (var u in _companyUpgrades.Concat(_rdUpgrades))
		{
			totalIncome += u.IncomeBonus * u.Level;
			totalSpeed += u.SpeedBonus * u.Level;
			totalXP += u.XPBonus * u.Level;
		}

		_globalIncomeMultiplier = 1.0 + totalIncome;
		_globalSpeedMultiplier = 1.0 + totalSpeed;
		_globalXPMultiplier = 1.0 + totalXP;
	}

	// --- ФОРМАТИРОВАНИЕ ЧИСЕЛ ---
	private string FormatNumber(double num)
	{
		if (num >= 1e9) return (num / 1e9).ToString("F2") + " B";
		if (num >= 1e6) return (num / 1e6).ToString("F2") + " M";
		if (num >= 1e3) return (num / 1e3).ToString("F2") + " K";
		return num.ToString("N0");
	}

	// --- UI ПОСТРОЕНИЕ ---
	private void BuildUI()
	{
		_canvasLayer = new CanvasLayer();
		AddChild(_canvasLayer);

		var bg = new ColorRect();
		bg.Color = COLOR_BG;
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_canvasLayer.AddChild(bg);

		var mainVertical = new VBoxContainer();
		mainVertical.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		mainVertical.AddThemeConstantOverride("separation", 0);
		_canvasLayer.AddChild(mainVertical);

		SetupTopBar(mainVertical);

		var midContainer = new HBoxContainer();
		midContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		midContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		mainVertical.AddChild(midContainer);

		SetupLeftSkillsPanel(midContainer);
		SetupCenterPanel(midContainer);
		SetupRightPanel(midContainer);

		SetupContractBar(mainVertical);
	}

		private void SetupTopBar(VBoxContainer parent)
	{
		var topBar = new PanelContainer();
		topBar.CustomMinimumSize = new Vector2(0, 80);
		topBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		var topStyle = new StyleBoxFlat();
		topStyle.BgColor = Color.FromHtml("#0f111a");
		topBar.AddThemeStyleboxOverride("panel", topStyle);
		parent.AddChild(topBar);

		var topLayout = new HBoxContainer();
		topLayout.Alignment = BoxContainer.AlignmentMode.Center;
		topLayout.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		topLayout.AddThemeConstantOverride("separation", 20);
		topBar.AddChild(topLayout);

		_balanceLabel = new Label();
		_balanceLabel.Text = "$ 0";
		_balanceLabel.AddThemeColorOverride("font_color", COLOR_ACCENT);
		_balanceLabel.AddThemeFontSizeOverride("font_size", 28);
		topLayout.AddChild(_balanceLabel);

		_runningSkillLabel = new Label();
		_runningSkillLabel.Text = "▶ 0/0 skills";
		_runningSkillLabel.AddThemeColorOverride("font_color", COLOR_TEXT);
		topLayout.AddChild(_runningSkillLabel);

		_companyLevelLabel = new Label();
		_companyLevelLabel.Text = $"Company Lv {_companyLevel}";
		_companyLevelLabel.AddThemeColorOverride("font_color", COLOR_TEXT_MUTED);
		topLayout.AddChild(_companyLevelLabel);

		_boostButton = new Button();
		_boostButton.Text = "⚡ BOOST (Ready)";
		_boostButton.CustomMinimumSize = new Vector2(120, 40);
		var boostStyle = new StyleBoxFlat();
		boostStyle.BgColor = Color.FromHtml("#fbbf24");
		_boostButton.AddThemeStyleboxOverride("normal", boostStyle);
		_boostButton.Pressed += () => ActivateBoost();
		topLayout.AddChild(_boostButton);

		_boostLabel = new Label();
		_boostLabel.Text = "";
		_boostLabel.AddThemeColorOverride("font_color", COLOR_TEXT_MUTED);
		_boostLabel.AddThemeFontSizeOverride("font_size", 12);
		topLayout.AddChild(_boostLabel);

		_menuToggleButton = new Button();
		_menuToggleButton.Text = "⚙️";
		_menuToggleButton.CustomMinimumSize = new Vector2(40, 40);
		_menuToggleButton.AddThemeFontSizeOverride("font_size", 20);
		
		var menuStyle = new StyleBoxFlat();
		menuStyle.BgColor = Color.FromHtml("#2e3440");
		menuStyle.CornerRadiusTopLeft = 8;
		menuStyle.CornerRadiusTopRight = 8;
		menuStyle.CornerRadiusBottomLeft = 8;
		menuStyle.CornerRadiusBottomRight = 8;
		_menuToggleButton.AddThemeStyleboxOverride("normal", menuStyle);
		_menuToggleButton.Pressed += () => ToggleMenu();
		topLayout.AddChild(_menuToggleButton);

		_menuSystem = new MenuSystem(this);
		_menuSystem.Initialize(_canvasLayer);
	}

	private void ToggleMenu()
	{
		if (_menuSystem != null)
		{
			if (_menuSystem.IsVisible())
				_menuSystem.Hide();
			else
				_menuSystem.Show();
		}
	}

	private void SetupLeftSkillsPanel(HBoxContainer parent)
	{
		var leftWrapper = CreateStyledPanel(COLOR_PANEL, 10);
		leftWrapper.CustomMinimumSize = new Vector2(220, 0);
		leftWrapper.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		parent.AddChild(leftWrapper);

		var leftScroll = new ScrollContainer();
		leftWrapper.AddChild(leftScroll);

		var skillContainer = new VBoxContainer();
		skillContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		skillContainer.AddThemeConstantOverride("separation", 8);
		leftScroll.AddChild(skillContainer);

		foreach (var skill in _skills)
		{
			var card = CreateStyledPanel(Color.FromHtml("#151a25"), 6);
			card.CustomMinimumSize = new Vector2(0, 65);
			card.MouseFilter = Control.MouseFilterEnum.Stop;
			skillContainer.AddChild(card);
			skill.CardPanel = card;

			if (!skill.IsUnlocked)
			{
				card.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.8f);
				var lockLabel = new Label();
				lockLabel.Text = "🔒 Locked";
				lockLabel.HorizontalAlignment = HorizontalAlignment.Center;
				lockLabel.VerticalAlignment = VerticalAlignment.Center;
				lockLabel.AddThemeColorOverride("font_color", COLOR_LOCKED);
				lockLabel.AddThemeFontSizeOverride("font_size", 16);
				card.AddChild(lockLabel);
				continue;
			}

			card.GuiInput += (e) => {
				if (e is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed)
					SelectSkill(skill);
			};

			var hBox = new HBoxContainer();
			hBox.AddThemeConstantOverride("separation", 10);
			card.AddChild(hBox);

			var icon = new Label();
			icon.Text = skill.IconName;
			icon.AddThemeFontSizeOverride("font_size", 24);
			hBox.AddChild(icon);

			var vBox = new VBoxContainer();
			vBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			hBox.AddChild(vBox);

			var titleLabel = new Label();
			titleLabel.Text = skill.Title;
			titleLabel.AddThemeColorOverride("font_color", COLOR_TEXT);
			vBox.AddChild(titleLabel);

			var lvlInc = new HBoxContainer();
			lvlInc.AddThemeConstantOverride("separation", 10);
			vBox.AddChild(lvlInc);

			skill.LevelLabel = new Label();
			skill.LevelLabel.Text = $"Lv {skill.Level}";
			skill.LevelLabel.AddThemeColorOverride("font_color", COLOR_TEXT_MUTED);
			skill.LevelLabel.AddThemeFontSizeOverride("font_size", 12);
			lvlInc.AddChild(skill.LevelLabel);

			skill.IncomeLabel = new Label();
			skill.IncomeLabel.Text = $"${FormatNumber(skill.GetCurrentIncome())}/s";
			skill.IncomeLabel.AddThemeColorOverride("font_color", COLOR_TEXT_MUTED);
			skill.IncomeLabel.AddThemeFontSizeOverride("font_size", 12);
			lvlInc.AddChild(skill.IncomeLabel);

			skill.UIProgressBar = new ProgressBar();
			skill.UIProgressBar.MaxValue = skill.GetMaxProgress();
			skill.UIProgressBar.ShowPercentage = false;
			skill.UIProgressBar.CustomMinimumSize = new Vector2(0, 4);
			skill.UIProgressBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			vBox.AddChild(skill.UIProgressBar);

			skill.PauseBtn = new Button();
			skill.PauseBtn.Text = "⏸";
			skill.PauseBtn.CustomMinimumSize = new Vector2(30, 30);
			skill.PauseBtn.Pressed += () => ToggleSkillPause(skill);
			hBox.AddChild(skill.PauseBtn);
		}
	}

	private void SetupCenterPanel(HBoxContainer parent)
	{
		var centerWrapper = CreateStyledPanel(COLOR_BG, 0);
		centerWrapper.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		parent.AddChild(centerWrapper);

		var centerSkillPanel = new VBoxContainer();
		centerSkillPanel.Alignment = BoxContainer.AlignmentMode.Center;
		centerSkillPanel.AddThemeConstantOverride("separation", 15);
		centerWrapper.AddChild(centerSkillPanel);

		var bigIcon = new Label();
		bigIcon.Text = _selectedSkill.IconName;
		bigIcon.AddThemeFontSizeOverride("font_size", 80);
		bigIcon.HorizontalAlignment = HorizontalAlignment.Center;
		centerSkillPanel.AddChild(bigIcon);

		var centerTitle = new Label();
		centerTitle.Text = _selectedSkill.Title;
		centerTitle.AddThemeColorOverride("font_color", COLOR_TEXT);
		centerTitle.AddThemeFontSizeOverride("font_size", 32);
		centerTitle.HorizontalAlignment = HorizontalAlignment.Center;
		centerSkillPanel.AddChild(centerTitle);

		_centerSkillLevel = new Label();
		_centerSkillLevel.HorizontalAlignment = HorizontalAlignment.Center;
		_centerSkillLevel.AddThemeColorOverride("font_color", COLOR_TEXT_MUTED);
		_centerSkillLevel.AddThemeFontSizeOverride("font_size", 18);
		centerSkillPanel.AddChild(_centerSkillLevel);

		_centerProgressBar = new ProgressBar();
		_centerProgressBar.MaxValue = _selectedSkill.GetMaxProgress();
		_centerProgressBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_centerProgressBar.CustomMinimumSize = new Vector2(0, 8);
		centerSkillPanel.AddChild(_centerProgressBar);

		_centerPauseBtn = new Button();
		_centerPauseBtn.Text = "⏸ Pause";
		_centerPauseBtn.CustomMinimumSize = new Vector2(200, 50);
		_centerPauseBtn.Pressed += () => ToggleSkillPause(_selectedSkill);
		centerSkillPanel.AddChild(_centerPauseBtn);

		_centerUpgradeBtn = new Button();
		_centerUpgradeBtn.CustomMinimumSize = new Vector2(200, 60);
		var upgStyle = new StyleBoxFlat();
		upgStyle.BgColor = Color.FromHtml("#3b82f6");
		upgStyle.CornerRadiusTopLeft = 8; upgStyle.CornerRadiusTopRight = 8; upgStyle.CornerRadiusBottomLeft = 8; upgStyle.CornerRadiusBottomRight = 8;
		_centerUpgradeBtn.AddThemeStyleboxOverride("normal", upgStyle);
		_centerUpgradeBtn.Pressed += () => TryUpgradeSkillLevel(_selectedSkill);
		centerSkillPanel.AddChild(_centerUpgradeBtn);

		UpdateCenterPanel();
		UpdateUI();
	}

	private void SetupRightPanel(HBoxContainer parent)
	{
		var rightWrapper = CreateStyledPanel(Color.FromHtml("#0f1219"), 10);
		rightWrapper.CustomMinimumSize = new Vector2(300, 0);
		rightWrapper.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		parent.AddChild(rightWrapper);

		var tabs = new TabContainer();
		tabs.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		tabs.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		tabs.AddThemeStyleboxOverride("tab_fg", new StyleBoxFlat { BgColor = COLOR_PANEL });
		rightWrapper.AddChild(tabs);

		var skillTab = new VBoxContainer();
		tabs.AddChild(skillTab); tabs.SetTabTitle(0, "Skill");
		var skillScroll = new ScrollContainer();
		skillScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		skillTab.AddChild(skillScroll);
		_skillTabContainer = new VBoxContainer();
		_skillTabContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_skillTabContainer.AddThemeConstantOverride("separation", 10);
		skillScroll.AddChild(_skillTabContainer);
		PopulateSkillTab();

		var companyTab = new VBoxContainer();
		tabs.AddChild(companyTab); tabs.SetTabTitle(1, "Company");
		SetupUpgradeList(companyTab, _companyUpgrades);

		var rdTab = new VBoxContainer();
		tabs.AddChild(rdTab); tabs.SetTabTitle(2, "R&D");
		SetupUpgradeList(rdTab, _rdUpgrades);
	}

	private void SetupContractBar(VBoxContainer parent)
	{
		var contractPanel = CreateStyledPanel(COLOR_BG, 0);
		contractPanel.CustomMinimumSize = new Vector2(0, 60);
		parent.AddChild(contractPanel);

		var contractVBox = new VBoxContainer();
		contractVBox.Alignment = BoxContainer.AlignmentMode.Center;
		contractVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		contractPanel.AddChild(contractVBox);

		var topRow = new HBoxContainer();
		topRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		contractVBox.AddChild(topRow);

		_contractLabel = new Label();
		_contractLabel.AddThemeColorOverride("font_color", COLOR_CONTRACT);
		_contractLabel.AddThemeFontSizeOverride("font_size", 14);
		topRow.AddChild(_contractLabel);

		var rewardLabel = new Label();
		rewardLabel.Text = "+ $500";
		rewardLabel.AddThemeColorOverride("font_color", COLOR_ACCENT);
		topRow.AddChild(rewardLabel);

		_contractBar = new ProgressBar();
		_contractBar.MaxValue = _contractMax;
		_contractBar.CustomMinimumSize = new Vector2(0, 8);
		_contractBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_contractBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = Color.FromHtml("#1f2630") });
		_contractBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = COLOR_CONTRACT });
		contractVBox.AddChild(_contractBar);
	}

	// --- ЛОГИКА ИГРЫ ---
	public override void _Process(double delta)
	{
		int runningCount = 0;
		if (_boostRemainingTime > 0) { _boostRemainingTime -= delta; if (_boostRemainingTime <= 0) { _boostRemainingTime = 0; _gameSpeedMultiplier = 1.0; _boostButton.Disabled = false; _boostButton.Text = "⚡ BOOST (Ready)"; _boostLabel.Text = ""; } else { _boostButton.Text = $"⚡ BOOST ({_boostRemainingTime:F0}s)"; } }
		else if (_boostCooldownRemaining > 0) { _boostCooldownRemaining -= delta; if (_boostCooldownRemaining <= 0) { _boostCooldownRemaining = 0; _boostButton.Disabled = false; _boostButton.Text = "⚡ BOOST (Ready)"; _boostLabel.Text = ""; } else { _boostButton.Disabled = true; _boostButton.Text = $"⚡ BOOST"; _boostLabel.Text = $"Cooldown: {TimeSpan.FromSeconds(_boostCooldownRemaining):hh\\:mm\\:ss}"; } }

		foreach (var skill in _skills)
		{
			if (skill.IsRunning && skill.IsUnlocked)
			{
				double effectiveSpeed = skill.Speed * _gameSpeedMultiplier * _globalSpeedMultiplier; 
				skill.CurrentProgress += effectiveSpeed * delta;
				runningCount++;
			}

			if (skill.CurrentProgress >= skill.GetMaxProgress())
			{
				skill.CurrentProgress = 0;
				double earned = skill.GetCurrentIncome() * _globalIncomeMultiplier;
				_totalBalance += earned;
				_contractProgress += earned;
				UpdateUI();
			}

			if (skill.UIProgressBar != null) 
			{
				skill.UIProgressBar.MaxValue = skill.GetMaxProgress();
				skill.UIProgressBar.Value = skill.CurrentProgress;
			}
		}

		_runningSkillLabel.Text = $"▶ {runningCount}/{_skills.Count} skills";
		if (_contractProgress >= _contractMax) { _contractProgress = 0; _totalBalance += _contractReward; GenerateNewContract(); UpdateUI(); }
		_contractBar.Value = _contractProgress;
		CheckCompanyLevel();
		UpdateCenterPanel();
	}

	private void ActivateBoost()
	{
		if (_boostCooldownRemaining > 0) return;
		_gameSpeedMultiplier = 2.0; _boostRemainingTime = 30.0; _boostCooldownRemaining = 86400.0;
		_boostButton.Disabled = true; _boostButton.Text = $"⚡ BOOST (Active)"; _boostLabel.Text = "";
	}

	private void SelectSkill(SkillData skill) { _selectedSkill = skill; UpdateCenterPanel(); PopulateSkillTab(); UpdateUI(); }

	private void PopulateSkillTab()
	{
		foreach (Node child in _skillTabContainer.GetChildren()) child.QueueFree();
		foreach (var upgr in _selectedSkill.SkillUpgrades)
		{
			var card = CreateStyledPanel(Color.FromHtml("#151e2b"), 6);
			_skillTabContainer.AddChild(card);
			if (upgr.Rarity == "Milestone") { var style = new StyleBoxFlat(); style.BgColor = Color.FromHtml("#151e2b"); style.BorderWidthLeft = 2; style.BorderWidthRight = 2; style.BorderWidthTop = 2; style.BorderWidthBottom = 2; style.BorderColor = COLOR_CONTRACT; style.CornerRadiusTopLeft = 6; style.CornerRadiusTopRight = 6; style.CornerRadiusBottomLeft = 6; style.CornerRadiusBottomRight = 6; card.AddThemeStyleboxOverride("panel", style); }
			else if (upgr.Rarity == "Capstone") { var style = new StyleBoxFlat(); style.BgColor = Color.FromHtml("#151e2b"); style.BorderWidthLeft = 2; style.BorderWidthRight = 2; style.BorderWidthTop = 2; style.BorderWidthBottom = 2; style.BorderColor = COLOR_CONTRACT; style.CornerRadiusTopLeft = 6; style.CornerRadiusTopRight = 6; style.CornerRadiusBottomLeft = 6; style.CornerRadiusBottomRight = 6; card.AddThemeStyleboxOverride("panel", style); }
			var vbox = new VBoxContainer(); card.AddChild(vbox);
			var titleRow = new HBoxContainer(); titleRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; vbox.AddChild(titleRow);
			var titleLabel = new Label(); titleLabel.Text = upgr.Title; titleLabel.AddThemeColorOverride("font_color", COLOR_TEXT); titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; titleRow.AddChild(titleLabel);
			if (upgr.Rarity == "Milestone") { var rarityLabel = new Label(); rarityLabel.Text = "MILESTONE"; rarityLabel.AddThemeColorOverride("font_color", COLOR_CONTRACT); rarityLabel.AddThemeFontSizeOverride("font_size", 10); titleRow.AddChild(rarityLabel); }
			else if (upgr.Rarity == "Capstone") { var rarityLabel = new Label(); rarityLabel.Text = "CAPSTONE"; rarityLabel.AddThemeColorOverride("font_color", COLOR_CONTRACT); rarityLabel.AddThemeFontSizeOverride("font_size", 10); titleRow.AddChild(rarityLabel); }
			var descLabel = new Label(); descLabel.Text = upgr.Description + $" (Req Lv {upgr.RequiredLevel})"; descLabel.AddThemeColorOverride("font_color", COLOR_TEXT_MUTED); descLabel.AddThemeFontSizeOverride("font_size", 12); vbox.AddChild(descLabel);
			upgr.BuyBtn = new Button(); upgr.BuyBtn.Text = upgr.IsBought ? "OWNED" : $"Buy ${upgr.Cost}"; upgr.BuyBtn.Pressed += () => { if (_totalBalance >= upgr.Cost && !upgr.IsBought && _selectedSkill.Level >= upgr.RequiredLevel) { _totalBalance -= upgr.Cost; upgr.IsBought = true; if (upgr.Type == "Money") _selectedSkill.BaseIncome *= (1 + upgr.BonusPercent); else if (upgr.Type == "Speed") _selectedSkill.Speed *= (1 + upgr.BonusPercent); else if (upgr.Type == "XP") _globalXPMultiplier *= (1 + upgr.BonusPercent); UpdateUI(); } }; vbox.AddChild(upgr.BuyBtn);
		}
	}

	private void SetupUpgradeList(VBoxContainer parent, List<GlobalUpgradeData> upgrades)
	{
		var layout = new VBoxContainer(); layout.AddThemeConstantOverride("separation", 10); layout.SizeFlagsVertical = Control.SizeFlags.ExpandFill; parent.AddChild(layout);
		var buyButtons = new HBoxContainer(); buyButtons.AddThemeConstantOverride("separation", 5); buyButtons.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; layout.AddChild(buyButtons);
		
		var addBuyModeBtn = (string text, BuyMode mode) => { 
			var btn = new Button(); 
			btn.Text = text; 
			btn.CustomMinimumSize = new Vector2(40, 30); 
			btn.Pressed += () => { _currentBuyMode = mode; UpdateUI(); }; 
			buyButtons.AddChild(btn); 
			_buyModeButtons.Add(btn); 
			return btn; 
		};
		addBuyModeBtn("x1", BuyMode.One); 
		addBuyModeBtn("x10", BuyMode.Ten); 
		addBuyModeBtn("x100", BuyMode.Hundred); 
		addBuyModeBtn("Max", BuyMode.Max);
		
		var scroll = new ScrollContainer(); scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill; layout.AddChild(scroll);
		var container = new VBoxContainer(); container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; container.AddThemeConstantOverride("separation", 8); scroll.AddChild(container);
		foreach (var upgr in upgrades)
		{
			var card = CreateStyledPanel(Color.FromHtml("#151e2b"), 6); container.AddChild(card);
			var vbox = new VBoxContainer(); card.AddChild(vbox);
			var titleLabel = new Label(); titleLabel.Text = upgr.Title; titleLabel.AddThemeColorOverride("font_color", COLOR_TEXT); vbox.AddChild(titleLabel);
			var descLabel = new Label(); descLabel.Text = upgr.Description; descLabel.AddThemeColorOverride("font_color", COLOR_TEXT_MUTED); descLabel.AddThemeFontSizeOverride("font_size", 12); vbox.AddChild(descLabel);
			upgr.LevelLabel = new Label(); upgr.LevelLabel.Text = $"Lv {upgr.Level}"; upgr.LevelLabel.AddThemeColorOverride("font_color", COLOR_TEXT); vbox.AddChild(upgr.LevelLabel);
			upgr.EffectLabel = new Label(); upgr.EffectLabel.Text = upgr.GetEffectText(); upgr.EffectLabel.AddThemeColorOverride("font_color", COLOR_ACCENT); upgr.EffectLabel.AddThemeFontSizeOverride("font_size", 12); vbox.AddChild(upgr.EffectLabel);
			upgr.UpgradeBtn = new Button(); upgr.UpgradeBtn.CustomMinimumSize = new Vector2(0, 40); upgr.UpgradeBtn.Pressed += () => TryBuyGlobalUpgradeBulk(upgr); vbox.AddChild(upgr.UpgradeBtn);
		}
	}

	// --- ОСНОВНАЯ ЛОГИКА ПОКУПОК ---
	private void TryBuyGlobalUpgradeBulk(GlobalUpgradeData upgrade)
	{
		if (upgrade.Level >= upgrade.MaxLevel) return;

		int count;
		if (_currentBuyMode == BuyMode.Max)
		{
			count = 0;
			double tempBalance = _totalBalance;
			for (int i = 0; i < upgrade.MaxLevel - upgrade.Level; i++)
			{
				double cost = upgrade.GetCost() * Math.Pow(upgrade.CostMultiplier, i);
				if (tempBalance >= cost)
				{
					tempBalance -= cost;
					count++;
				}
				else break;
			}
		}
		else
		{
			count = _currentBuyMode == BuyMode.One ? 1 :
					_currentBuyMode == BuyMode.Ten ? 10 :
					_currentBuyMode == BuyMode.Hundred ? 100 : 0;
		}

		bool bought = false;
		for (int i = 0; i < count; i++)
		{
			if (upgrade.Level >= upgrade.MaxLevel) break;
			
			double currentCost = upgrade.GetCost(); 
			
			if (_totalBalance >= currentCost)
			{
				_totalBalance -= currentCost;
				upgrade.Level++;
				bought = true;
			}
			else break;
		}

		if (bought)
		{
			RecalculateModifiers();
			UpdateUI();
			SaveGame();
		}
	}

	private void TryUpgradeSkillLevel(SkillData skill) 
	{ 
		double cost = skill.GetUpgradeCost(); 
		if (_totalBalance >= cost) 
		{ 
			_totalBalance -= cost; 
			skill.Level++; 
			skill.Speed *= 1.12; 
			UpdateUI(); 
			SaveGame();
		} 
	}

	private void ToggleSkillPause(SkillData skill) 
	{ 
		if (!skill.IsUnlocked) return;
		skill.IsRunning = !skill.IsRunning; 
		skill.PauseBtn.Text = skill.IsRunning ? "⏸" : "▶"; 
		if (_selectedSkill == skill) UpdateCenterPanel(); 
	}
	
	private void CheckCompanyLevel() 
	{ 
		double needed = Math.Pow(2, _companyLevel) * 1000; 
		if (_totalBalance >= needed) 
		{ 
			_companyLevel++; 
			_companyLevelLabel.Text = $"Company Lv {_companyLevel}"; 
			CheckUnlockTiers();
			UpdateUI();
			SaveGame();
		} 
	}
	
	private void GenerateNewContract() { var rnd = new Random(); _contractMax = rnd.Next(5000, 15000); _contractReward = _contractMax * 0.1; _contractLabel.Text = $"CONTRACT: Earn ${FormatNumber(_contractMax)}"; }
	
	private void UpdateCenterPanel() 
	{ 
		if (_selectedSkill == null || _centerSkillLevel == null || _centerProgressBar == null || _centerUpgradeBtn == null) return; 
		_centerSkillLevel.Text = $"Level {_selectedSkill.Level} • {FormatNumber(_selectedSkill.CurrentProgress)}/{FormatNumber(_selectedSkill.GetMaxProgress())}"; 
		_centerProgressBar.MaxValue = _selectedSkill.GetMaxProgress();
		_centerProgressBar.Value = _selectedSkill.CurrentProgress; 
		_centerPauseBtn.Text = _selectedSkill.IsRunning ? "⏸ Pause" : "▶ Resume"; 
		_centerUpgradeBtn.Text = $"Upgrade\n${FormatNumber(_selectedSkill.GetUpgradeCost())}"; 
		_centerUpgradeBtn.Disabled = _totalBalance < _selectedSkill.GetUpgradeCost(); 
	}
	
	private void UpdateUI() 
	{ 
		if (_balanceLabel == null) return; 
		_balanceLabel.Text = $"$ {FormatNumber(_totalBalance)}"; 
		foreach (var skill in _skills) { skill.LevelLabel.Text = $"Lv {skill.Level}"; skill.IncomeLabel.Text = $"${FormatNumber(skill.GetCurrentIncome())}/s"; } 
		foreach (var upgr in _selectedSkill.SkillUpgrades) { if (upgr.BuyBtn != null && !upgr.IsBought) upgr.BuyBtn.Disabled = _totalBalance < upgr.Cost || _selectedSkill.Level < upgr.RequiredLevel; } 
		
		foreach (var btn in _buyModeButtons) 
		{ 
			var style = new StyleBoxFlat(); 
			string btnText = btn.Text; 
			if (btnText == _currentBuyMode.ToString().Replace("One","x1").Replace("Ten","x10").Replace("Hundred","x100").Replace("Max","Max")) 
				style.BgColor = new Color("#3b82f6"); 
			else 
				style.BgColor = COLOR_PANEL; 
			btn.AddThemeStyleboxOverride("normal", style); 
		} 
		
		foreach (var upgr in _companyUpgrades.Concat(_rdUpgrades)) 
		{ 
			if (upgr.UpgradeBtn == null) continue; 
			if (upgr.Level >= upgr.MaxLevel) { upgr.UpgradeBtn.Disabled = true; continue; } 
			
			bool canBuyAll = true;
			int desiredCount = _currentBuyMode == BuyMode.Max ? upgr.MaxLevel - upgr.Level :
						_currentBuyMode == BuyMode.Hundred ? 100 :
						_currentBuyMode == BuyMode.Ten ? 10 : 1;
			
			if (_currentBuyMode == BuyMode.Max)
			{
				desiredCount = 0;
				double tempBalance = _totalBalance;
				for (int i = 0; i < upgr.MaxLevel - upgr.Level; i++)
				{
					double cost = upgr.GetCost() * Math.Pow(upgr.CostMultiplier, i);
					if (tempBalance >= cost) { tempBalance -= cost; desiredCount++; }
					else break;
				}
			}

			double tempBalanceCheck = _totalBalance;
			for (int i = 0; i < desiredCount; i++)
			{
				if (upgr.Level + i >= upgr.MaxLevel) break;
				double calcCost = upgr.GetCost() * Math.Pow(upgr.CostMultiplier, i);
				if (tempBalanceCheck < calcCost) { canBuyAll = false; break; }
				tempBalanceCheck -= calcCost;
			}

			if (desiredCount > 1 && !canBuyAll) upgr.UpgradeBtn.Disabled = true;
			else upgr.UpgradeBtn.Disabled = _totalBalance < upgr.GetCost();

			double displayCost = upgr.GetCost();
			int count = desiredCount;
			upgr.UpgradeBtn.Text = count > 1 ? $"Buy x{count}\n${FormatNumber(displayCost * count)}" : $"Buy\n${FormatNumber(displayCost)}";
			upgr.LevelLabel.Text = $"Lv {upgr.Level}";
			upgr.EffectLabel.Text = upgr.GetEffectText();
		} 
	}

	public PanelContainer CreateStyledPanel(Color color, int radius)
	{
		var panel = new PanelContainer();
		var style = new StyleBoxFlat();
		style.BgColor = color;
		style.CornerRadiusTopLeft = radius; style.CornerRadiusTopRight = radius; style.CornerRadiusBottomLeft = radius; style.CornerRadiusBottomRight = radius;
		style.ContentMarginLeft = 10; style.ContentMarginTop = 10; style.ContentMarginRight = 10; style.ContentMarginBottom = 10;
		panel.AddThemeStyleboxOverride("panel", style);
		return panel;
	}

	// --- ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ МЕНЮ ---
	public void PerformRefound() 
	{ 
		_totalBalance = 0;
		_companyLevel = 1;
		foreach (var skill in _skills)
		{
			skill.Level = 1;
			skill.CurrentProgress = 0;
			if (skill.Title == "Coding") skill.Speed = 60;
			else if (skill.Title == "Design") skill.Speed = 40;
			else if (skill.Title == "Marketing") skill.Speed = 25;
			else if (skill.Title == "Sales") skill.Speed = 15;
			else if (skill.Title == "Support") skill.Speed = 30;
			else if (skill.Title == "Management") skill.Speed = 10;
		}
		UpdateUI();
		SaveGame();
	}

	public void DeleteSaveAndReset() 
	{ 
		_totalBalance = 0;
		_companyLevel = 1;
		foreach (var skill in _skills)
		{
			skill.Level = 1;
			skill.CurrentProgress = 0;
			if (skill.Title == "Coding") skill.Speed = 60;
			else if (skill.Title == "Design") skill.Speed = 40;
			else if (skill.Title == "Marketing") skill.Speed = 25;
			else if (skill.Title == "Sales") skill.Speed = 15;
			else if (skill.Title == "Support") skill.Speed = 30;
			else if (skill.Title == "Management") skill.Speed = 10;
		}
		UpdateUI();
		SaveGame();
		GetTree().ReloadCurrentScene();
	}

	public void RenameCompany(string newName) { _companyName = newName; }
	public void SetMasterVolume(float volume) { GD.Print($"Master volume set to: {volume * 100:F0}%"); }
}

// ==========================================
// КЛАСС СИСТЕМЫ МЕНЮ (ФИНАЛЬНЫЙ)
// ==========================================
public class MenuSystem
{
	private PanelContainer _mainMenuPanel;
	private CanvasLayer _parentCanvas;
	private VBoxContainer _mainMenuList;
	private GameBootstrapper _game; 

	private Dictionary<string, Control> _panels = new Dictionary<string, Control>();
	private Control _currentPanel;

	public MenuSystem(GameBootstrapper game)
	{
		_game = game;
	}

	public void Initialize(CanvasLayer canvas)
	{
		_parentCanvas = canvas;
		
		_mainMenuPanel = new PanelContainer();
		_mainMenuPanel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		_mainMenuPanel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		_mainMenuPanel.CustomMinimumSize = new Vector2(400, 500);
		_mainMenuPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
		_mainMenuPanel.Visible = false;
		
		var menuStyle = new StyleBoxFlat();
		menuStyle.BgColor = Color.FromHtml("#1e222b");
		menuStyle.CornerRadiusTopLeft = 12; menuStyle.CornerRadiusTopRight = 12; menuStyle.CornerRadiusBottomLeft = 12; menuStyle.CornerRadiusBottomRight = 12;
		menuStyle.BorderWidthLeft = 1; menuStyle.BorderWidthRight = 1; menuStyle.BorderWidthTop = 1; menuStyle.BorderWidthBottom = 1;
		menuStyle.BorderColor = Color.FromHtml("#2e3440");
		_mainMenuPanel.AddThemeStyleboxOverride("panel", menuStyle);
		
		var menuVBox = new VBoxContainer();
		menuVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		menuVBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		menuVBox.AddThemeConstantOverride("separation", 0);
		_mainMenuPanel.AddChild(menuVBox);

		var header = new PanelContainer();
		header.CustomMinimumSize = new Vector2(0, 50);
		header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		menuVBox.AddChild(header);
		
		var headerLayout = new HBoxContainer();
		headerLayout.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		header.AddChild(headerLayout);
		
		var titleIcon = new Label();
		titleIcon.Text = "🟩";
		titleIcon.AddThemeColorOverride("font_color", Colors.LightGreen);
		titleIcon.AddThemeFontSizeOverride("font_size", 16);
		headerLayout.AddChild(titleIcon);
		
		var titleLabel = new Label();
		titleLabel.Text = "Menu";
		titleLabel.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0"));
		titleLabel.AddThemeFontSizeOverride("font_size", 20);
		titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		headerLayout.AddChild(titleLabel);
		
		var closeBtn = new Button();
		closeBtn.Text = "✕";
		closeBtn.AddThemeColorOverride("font_color", Color.FromHtml("#94a3b8"));
		closeBtn.AddThemeFontSizeOverride("font_size", 20);
		closeBtn.Pressed += () => Hide();
		headerLayout.AddChild(closeBtn);
		
		var line = new ColorRect();
		line.Color = Color.FromHtml("#2e3440");
		line.CustomMinimumSize = new Vector2(0, 1);
		line.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		menuVBox.AddChild(line);

		var mainContentVBox = new VBoxContainer();
		mainContentVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		mainContentVBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		mainContentVBox.AddThemeConstantOverride("separation", 0);
		menuVBox.AddChild(mainContentVBox);

		_mainMenuList = new VBoxContainer();
		_mainMenuList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_mainMenuList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_mainMenuList.AddThemeConstantOverride("separation", 8);
		_mainMenuList.AddThemeConstantOverride("margin_left", 15);
		_mainMenuList.AddThemeConstantOverride("margin_right", 15);
		_mainMenuList.AddThemeConstantOverride("margin_top", 15);
		_mainMenuList.AddThemeConstantOverride("margin_bottom", 15);
		mainContentVBox.AddChild(_mainMenuList);
		_panels["Main"] = _mainMenuList;
		
		AddMenuItem(_mainMenuList, "📝", "Company Name", "Rename your company", () => ShowPanel("CompanyName"));
		AddMenuItem(_mainMenuList, "⚡", "Founders Club", "Refound for Founder Points & spend them", () => ShowPanel("FoundersClub"));
		AddMenuItem(_mainMenuList, "🏆", "Records", "Achievements & global Hiscores", () => ShowPanel("Records"));
		AddMenuItem(_mainMenuList, "⚙️", "Settings", "Audio, display scale & theme", () => ShowPanel("Settings"));
		AddMenuItem(_mainMenuList, "🚀", "Replay Tutorial", "Watch the intro walkthrough again", () => ShowPanel("Dummy"));
		AddMenuItem(_mainMenuList, "💾", "Manage Saves", "Switch between your cloud and local saves", () => ShowPanel("ManageSaves"));
		AddMenuItem(_mainMenuList, "🗑️", "Delete Save", "Erase all progress and start fresh", () => ShowPanel("DeleteSave"));
		AddMenuItem(_mainMenuList, "ℹ️", "Game Info", "Version, terms and privacy policy", () => ShowPanel("GameInfo"));
		AddMenuItem(_mainMenuList, "🚪", "Save & Exit", "Save your progress and close the game", () => { Hide(); _game.GetTree().Quit(); });

		CreateCompanyNamePanel("CompanyName");
		CreateFoundersClubPanel("FoundersClub");
		CreateRecordsPanel("Records");
		CreateSettingsPanel("Settings");
		CreateDeleteSavePanel("DeleteSave");
		CreateGameInfoPanel("GameInfo");
		CreateDummyPanel("ManageSaves", "Manage Saves", "Cloud save functionality requires Steam integration.");
		
		foreach(var kvp in _panels)
		{
			if(kvp.Key != "Main")
			{
				kvp.Value.Visible = false;
				mainContentVBox.AddChild(kvp.Value);
			}
		}

		_parentCanvas.AddChild(_mainMenuPanel);
		_currentPanel = _mainMenuList;
	}

	private void AddMenuItem(VBoxContainer parent, string icon, string title, string desc, Action action)
	{
		var btn = new Button();
		btn.CustomMinimumSize = new Vector2(0, 60);
		btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		
		var style = new StyleBoxFlat();
		style.BgColor = Color.FromHtml("#151e2b");
		style.CornerRadiusTopLeft = 8; style.CornerRadiusTopRight = 8; style.CornerRadiusBottomLeft = 8; style.CornerRadiusBottomRight = 8;
		style.BorderWidthLeft = 1; style.BorderWidthRight = 1; style.BorderWidthTop = 1; style.BorderWidthBottom = 1;
		style.BorderColor = Color.FromHtml("#2e3440");
		btn.AddThemeStyleboxOverride("normal", style);
		
		var hBox = new HBoxContainer();
		hBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		btn.AddChild(hBox);

		var iconLabel = new Label();
		iconLabel.Text = icon;
		iconLabel.AddThemeFontSizeOverride("font_size", 20);
		iconLabel.CustomMinimumSize = new Vector2(40, 40);
		hBox.AddChild(iconLabel);

		var vBox = new VBoxContainer();
		vBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		hBox.AddChild(vBox);

		var titleLbl = new Label();
		titleLbl.Text = title;
		titleLbl.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0"));
		titleLbl.AddThemeFontSizeOverride("font_size", 16);
		vBox.AddChild(titleLbl);

		var descLbl = new Label();
		descLbl.Text = desc;
		descLbl.AddThemeColorOverride("font_color", Color.FromHtml("#94a3b8"));
		descLbl.AddThemeFontSizeOverride("font_size", 12);
		vBox.AddChild(descLbl);

		var arrow = new Label();
		arrow.Text = "›";
		arrow.AddThemeColorOverride("font_color", Color.FromHtml("#94a3b8"));
		arrow.AddThemeFontSizeOverride("font_size", 24);
		hBox.AddChild(arrow);

		btn.Pressed += action;
		parent.AddChild(btn);
	}

	private void CreateCompanyNamePanel(string id)
	{
		var panel = new VBoxContainer();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		panel.AddThemeConstantOverride("separation", 10);
		panel.AddThemeConstantOverride("margin_left", 15);
		panel.AddThemeConstantOverride("margin_right", 15);
		panel.AddThemeConstantOverride("margin_top", 15);
		panel.AddThemeConstantOverride("margin_bottom", 15);

		var title = new Label(); title.Text = "📝 Company Name"; title.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0")); title.AddThemeFontSizeOverride("font_size", 22); panel.AddChild(title);
		
		var input = new LineEdit();
		input.PlaceholderText = "Enter new company name";
		panel.AddChild(input);

		var confirmBtn = new Button();
		confirmBtn.Text = "Set Name";
		confirmBtn.Pressed += () => {
			if (!string.IsNullOrEmpty(input.Text))
			{
				_game.RenameCompany(input.Text);
				ShowPanel("Main");
			}
		};
		panel.AddChild(confirmBtn);

		var backBtn = new Button(); backBtn.Text = "← Back"; backBtn.Pressed += () => ShowPanel("Main"); panel.AddChild(backBtn);
		_panels[id] = panel;
	}

	private void CreateFoundersClubPanel(string id)
	{
		var panel = new VBoxContainer();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		panel.AddThemeConstantOverride("separation", 10);
		panel.AddThemeConstantOverride("margin_left", 15);
		panel.AddThemeConstantOverride("margin_right", 15);
		panel.AddThemeConstantOverride("margin_top", 15);
		panel.AddThemeConstantOverride("margin_bottom", 15);

		var title = new Label(); title.Text = "🟨 Founders Club"; title.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0")); title.AddThemeFontSizeOverride("font_size", 22); panel.AddChild(title);
		var desc = new Label(); desc.Text = "Refound to reset your run for Founder Points..."; desc.AddThemeColorOverride("font_color", Color.FromHtml("#94a3b8")); desc.AddThemeFontSizeOverride("font_size", 14); panel.AddChild(desc);
		panel.AddChild(new ColorRect { Color = Color.FromHtml("#2e3440"), CustomMinimumSize = new Vector2(0,1) });

		var card = _game.CreateStyledPanel(Color.FromHtml("#151e2b"), 8);
		var vbox = new VBoxContainer(); card.AddChild(vbox);
		var row = new HBoxContainer(); row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; vbox.AddChild(row);
		var lbl = new Label(); lbl.Text = "Founder Points"; lbl.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0")); lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; row.AddChild(lbl);
		var val = new Label(); val.Text = "0"; val.AddThemeColorOverride("font_color", Color.FromHtml("#fbbf24")); row.AddChild(val);
		panel.AddChild(card);

		var refoundBtn = new Button();
		refoundBtn.Text = "Refound for +1 FP";
		refoundBtn.Pressed += () => {
			_game.PerformRefound();
			ShowPanel("Main");
		};
		panel.AddChild(refoundBtn);

		var backBtn = new Button(); backBtn.Text = "← Back"; backBtn.Pressed += () => ShowPanel("Main"); panel.AddChild(backBtn);
		_panels[id] = panel;
	}

	private void CreateRecordsPanel(string id)
	{
		var panel = new VBoxContainer();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		panel.AddThemeConstantOverride("separation", 10);
		panel.AddThemeConstantOverride("margin_left", 15);
		panel.AddThemeConstantOverride("margin_right", 15);
		panel.AddThemeConstantOverride("margin_top", 15);
		panel.AddThemeConstantOverride("margin_bottom", 15);

		var title = new Label(); title.Text = "🟨 Records"; title.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0")); title.AddThemeFontSizeOverride("font_size", 22); panel.AddChild(title);
		AddSubMenuButton(panel, "🏆", "Achievements", "Track your progress and milestones", () => ShowPanel("Dummy"));
		AddSubMenuButton(panel, "🏅", "Hiscores", "Your all-time stats & global ranks", () => ShowPanel("Dummy"));
		var backBtn = new Button(); backBtn.Text = "← Back"; backBtn.Pressed += () => ShowPanel("Main"); panel.AddChild(backBtn);
		_panels[id] = panel;
	}

	private void CreateSettingsPanel(string id)
	{
		var panel = new VBoxContainer();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		panel.AddThemeConstantOverride("separation", 10);
		panel.AddThemeConstantOverride("margin_left", 15);
		panel.AddThemeConstantOverride("margin_right", 15);
		panel.AddThemeConstantOverride("margin_top", 15);
		panel.AddThemeConstantOverride("margin_bottom", 15);

		var title = new Label(); title.Text = "🟩 Settings"; title.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0")); title.AddThemeFontSizeOverride("font_size", 22); panel.AddChild(title);
		AddSubMenuButton(panel, "🎵", "Music & Sounds", "Volume, mute and the lofi player", () => ShowPanel("Dummy"));
		AddSubMenuButton(panel, "📱", "Display", "Make the UI and text bigger or smaller", () => ShowPanel("Dummy"));
		AddSubMenuButton(panel, "👁️", "Theme", "Choose the look of the app", () => ShowPanel("Dummy"));

		var volumeRow = new HBoxContainer(); volumeRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; panel.AddChild(volumeRow);
		var volumeLabel = new Label(); volumeLabel.Text = "Master Volume"; volumeLabel.AddThemeColorOverride("font_color", Color.FromHtml("#94a3b8")); volumeRow.AddChild(volumeLabel);
		var slider = new HSlider(); slider.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; slider.Value = 0.5; 
		slider.ValueChanged += (val) => { _game.SetMasterVolume((float)val); };
		volumeRow.AddChild(slider);

		var backBtn = new Button(); backBtn.Text = "← Back"; backBtn.Pressed += () => ShowPanel("Main"); panel.AddChild(backBtn);
		_panels[id] = panel;
	}

	private void CreateDeleteSavePanel(string id)
	{
		var panel = new VBoxContainer();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		panel.AddThemeConstantOverride("separation", 10);
		panel.AddThemeConstantOverride("margin_left", 15);
		panel.AddThemeConstantOverride("margin_right", 15);
		panel.AddThemeConstantOverride("margin_top", 15);
		panel.AddThemeConstantOverride("margin_bottom", 15);

		var title = new Label(); title.Text = "🟥 Delete Save?"; title.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0")); title.AddThemeFontSizeOverride("font_size", 22); panel.AddChild(title);
		var desc = new Label(); desc.Text = "This permanently erases all progress..."; desc.AddThemeColorOverride("font_color", Color.FromHtml("#94a3b8")); panel.AddChild(desc);
		
		var btnRow = new HBoxContainer(); btnRow.Alignment = BoxContainer.AlignmentMode.End; btnRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; panel.AddChild(btnRow);
		var cancelBtn = new Button(); cancelBtn.Text = "Cancel"; cancelBtn.Pressed += () => ShowPanel("Main"); btnRow.AddChild(cancelBtn);
		var delBtn = new Button(); delBtn.Text = "Delete Save"; delBtn.AddThemeColorOverride("font_color", Colors.Red); delBtn.Pressed += () => { _game.DeleteSaveAndReset(); }; btnRow.AddChild(delBtn);
		_panels[id] = panel;
	}

	private void CreateGameInfoPanel(string id)
	{
		var panel = new VBoxContainer();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		panel.AddThemeConstantOverride("separation", 10);
		panel.AddThemeConstantOverride("margin_left", 15);
		panel.AddThemeConstantOverride("margin_right", 15);
		panel.AddThemeConstantOverride("margin_top", 15);
		panel.AddThemeConstantOverride("margin_bottom", 15);

		var title = new Label(); title.Text = "🟩 Game Info"; title.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0")); title.AddThemeFontSizeOverride("font_size", 22); panel.AddChild(title);
		var desc = new Label(); desc.Text = "Idle Startup\nVersion 1.0.183"; desc.HorizontalAlignment = HorizontalAlignment.Center; desc.AddThemeColorOverride("font_color", Color.FromHtml("#10b981")); desc.AddThemeFontSizeOverride("font_size", 18); panel.AddChild(desc);
		AddSubMenuButton(panel, "ℹ️", "Terms of Service", "How you may use the game", () => ShowPanel("Dummy"));
		AddSubMenuButton(panel, "ℹ️", "Privacy Policy", "What data the game handles", () => ShowPanel("Dummy"));
		var backBtn = new Button(); backBtn.Text = "← Back"; backBtn.Pressed += () => ShowPanel("Main"); panel.AddChild(backBtn);
		_panels[id] = panel;
	}

	private void CreateDummyPanel(string id, string title, string content)
	{
		var panel = new VBoxContainer();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		panel.AddThemeConstantOverride("separation", 10);
		panel.AddThemeConstantOverride("margin_left", 15);
		panel.AddThemeConstantOverride("margin_right", 15);
		panel.AddThemeConstantOverride("margin_top", 15);
		panel.AddThemeConstantOverride("margin_bottom", 15);
		
		var lbl = new Label(); lbl.Text = title; lbl.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0")); lbl.AddThemeFontSizeOverride("font_size", 22); panel.AddChild(lbl);
		var contentLbl = new Label(); contentLbl.Text = content; contentLbl.AddThemeColorOverride("font_color", Color.FromHtml("#94a3b8")); panel.AddChild(contentLbl);
		var backBtn = new Button(); backBtn.Text = "← Back"; backBtn.Pressed += () => ShowPanel("Main"); panel.AddChild(backBtn);
		_panels[id] = panel;
	}

	private void AddSubMenuButton(VBoxContainer parent, string icon, string title, string desc, Action action)
	{
		var btn = new Button();
		btn.CustomMinimumSize = new Vector2(0, 50);
		btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		var style = new StyleBoxFlat();
		style.BgColor = Color.FromHtml("#151e2b");
		style.CornerRadiusTopLeft = 8; style.CornerRadiusTopRight = 8; style.CornerRadiusBottomLeft = 8; style.CornerRadiusBottomRight = 8;
		style.BorderWidthLeft = 1; style.BorderWidthRight = 1; style.BorderWidthTop = 1; style.BorderWidthBottom = 1;
		style.BorderColor = Color.FromHtml("#2e3440");
		btn.AddThemeStyleboxOverride("normal", style);
		
		var hBox = new HBoxContainer(); hBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; btn.AddChild(hBox);
		var iconLabel = new Label(); iconLabel.Text = icon; iconLabel.AddThemeFontSizeOverride("font_size", 20); iconLabel.CustomMinimumSize = new Vector2(40, 40); hBox.AddChild(iconLabel);
		var vBox = new VBoxContainer(); vBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; hBox.AddChild(vBox);
		var titleLbl = new Label(); titleLbl.Text = title; titleLbl.AddThemeColorOverride("font_color", Color.FromHtml("#e2e8f0")); titleLbl.AddThemeFontSizeOverride("font_size", 16); vBox.AddChild(titleLbl);
		var descLbl = new Label(); descLbl.Text = desc; descLbl.AddThemeColorOverride("font_color", Color.FromHtml("#94a3b8")); descLbl.AddThemeFontSizeOverride("font_size", 12); vBox.AddChild(descLbl);
		var arrow = new Label(); arrow.Text = "›"; arrow.AddThemeColorOverride("font_color", Color.FromHtml("#94a3b8")); arrow.AddThemeFontSizeOverride("font_size", 24); hBox.AddChild(arrow);

		btn.Pressed += action;
		parent.AddChild(btn);
	}

	private void ShowPanel(string id)
	{
		if (_panels.ContainsKey(id))
		{
			if (_currentPanel != null) _currentPanel.Visible = false;
			_currentPanel = _panels[id];
			_currentPanel.Visible = true;
		}
	}

	public void Show()
	{
		_mainMenuPanel.Visible = true;
		ShowPanel("Main");
	}

	public void Hide()
	{
		_mainMenuPanel.Visible = false;
	}

	public bool IsVisible()
	{
		return _mainMenuPanel.Visible;
	}
}
