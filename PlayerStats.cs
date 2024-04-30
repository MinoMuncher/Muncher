public class PlayerStats
{
	public static PlayerStats operator +(PlayerStats a, PlayerStats b)
	{
		PlayerStats stats = new PlayerStats
		{
			wellColumns = new int[a.wellColumns.Length]
		};
		for (int i = 0; i < a.wellColumns.Length; i++)
			stats.wellColumns[i] = a.wellColumns[i] + b.wellColumns[i];

		stats.clearTypes = a.clearTypes + b.clearTypes;
		stats.tEfficiency = a.tEfficiency + b.tEfficiency;
		stats.iEfficiency = a.iEfficiency + b.iEfficiency;
		stats.cheeseApl = a.cheeseApl + b.cheeseApl;
		stats.downstackApl = a.downstackApl + b.downstackApl;
		stats.upstackApl = a.upstackApl + b.upstackApl;
		stats.apl = a.apl + b.apl;
		stats.app = a.app + b.app;
		stats.kpp = a.kpp + b.kpp;
		stats.kps = a.kps + b.kps;
		stats.stackHeight = a.stackHeight + b.stackHeight;
		stats.garbageHeight = a.garbageHeight + b.garbageHeight;
		stats.spikeEfficiency = a.spikeEfficiency + b.spikeEfficiency;
		stats.apm = a.apm + b.apm;
		stats.openerApm = a.openerApm + b.openerApm;
		stats.midgameApm = a.midgameApm + b.midgameApm;
		stats.pps = a.pps + b.pps;
		stats.midgamePps = a.midgamePps + b.midgamePps;
		stats.openerPps = a.openerPps + b.openerPps;
		stats.btbWellshifts = a.btbWellshifts + b.btbWellshifts;
		stats.btbChainEfficiency = a.btbChainEfficiency + b.btbChainEfficiency;
		stats.btbChain = a.btbChain + b.btbChain;
		stats.btbChainApm = a.btbChainApm + b.btbChainApm;
		stats.btbChainAttack = a.btbChainAttack + b.btbChainAttack;
		stats.btbChainWellshifts = a.btbChainWellshifts + b.btbChainWellshifts;
		stats.btbChainApp = a.btbChainApp + b.btbChainApp;
		stats.maxBtb = a.maxBtb + b.maxBtb;
		stats.maxBtbAttack = a.maxBtbAttack + b.maxBtbAttack;
		stats.comboChainEfficiency = a.comboChainEfficiency + b.comboChainEfficiency;
		stats.comboChain = a.comboChain + b.comboChain;
		stats.comboChainApm = a.comboChainApm + b.comboChainApm;
		stats.comboChainAttack = a.comboChainAttack + b.comboChainAttack;
		stats.comboChainApp = a.comboChainApp + b.comboChainApp;
		stats.maxCombo = a.maxCombo + b.maxCombo;
		stats.maxComboAttack = a.maxComboAttack + b.maxComboAttack;
		stats.averageSpikePotential = a.averageSpikePotential + b.averageSpikePotential;
		stats.averageDefencePotential = a.averageDefencePotential + b.averageDefencePotential;
		stats.ppsVariance = a.ppsVariance + b.ppsVariance;
		stats.blockfishScore = a.blockfishScore + b.blockfishScore;
		stats.burstPps = a.burstPps + b.burstPps;
		stats.attackDelayRate = a.attackDelayRate + b.attackDelayRate;
		stats.preAttackDelayRate = a.preAttackDelayRate + b.preAttackDelayRate;

		return stats;
	}


	public int[] wellColumns { get; set; }
	public ClearTypes clearTypes { get; set; }
	public double tEfficiency { get; set; }
	public double iEfficiency { get; set; }
	public double cheeseApl { get; set; }
	public double downstackApl { get; set; }
	public double upstackApl { get; set; }
	public double apl { get; set; }
	public double app { get; set; }
	public double kpp { get; set; }
	public double kps { get; set; }
	public double stackHeight { get; set; }
	public double garbageHeight { get; set; }
	public double spikeEfficiency { get; set; }
	public double apm { get; set; }
	public double openerApm { get; set; }
	public double midgameApm { get; set; }
	public double pps { get; set; }
	public double midgamePps { get; set; }
	public double openerPps { get; set; }
	public double btbWellshifts { get; set; }
	public double btbChainEfficiency { get; set; }
	public double btbChain { get; set; }
	public double btbChainApm { get; set; }
	public double btbChainAttack { get; set; }
	public double btbChainWellshifts { get; set; }
	public double btbChainApp { get; set; }
	public double maxBtb { get; set; }
	public double maxBtbAttack { get; set; }
	public double comboChainEfficiency { get; set; }
	public double comboChain { get; set; }
	public double comboChainApm { get; set; }
	public double comboChainAttack { get; set; }
	public double comboChainApp { get; set; }
	public double maxCombo { get; set; }
	public double maxComboAttack { get; set; }
	public double averageSpikePotential { get; set; }
	public double averageDefencePotential { get; set; }
	public double ppsVariance { get; set; }
	public double blockfishScore { get; set; }
	public double burstPps { get; set; }
	public double attackDelayRate { get; set; }
	public double preAttackDelayRate { get; set; }
}

public class ClearTypes
{
	public static ClearTypes operator +(ClearTypes a, ClearTypes b)
	{
		ClearTypes obj = new ClearTypes
		{
			TSPIN_SINGLE = a.TSPIN_SINGLE + b.TSPIN_SINGLE,
			TSPIN_MINI_SINGLE = a.TSPIN_MINI_SINGLE + b.TSPIN_MINI_SINGLE,
			TSPIN_MINI_DOUBLE = a.TSPIN_MINI_DOUBLE + b.TSPIN_MINI_DOUBLE,
			TRIPLE = a.TRIPLE + b.TRIPLE,
			QUAD = a.QUAD + b.QUAD,
			TSPIN_QUAD = a.TSPIN_QUAD + b.TSPIN_QUAD,
			TSPIN = a.TSPIN + b.TSPIN,
			PENTA = a.PENTA + b.PENTA,
			NONE = a.NONE + b.NONE,
			SINGLE = a.SINGLE + b.SINGLE,
			DOUBLE = a.DOUBLE + b.DOUBLE,
			PERFECT_CLEAR = a.PERFECT_CLEAR + b.PERFECT_CLEAR,
			TSPIN_MINI = a.TSPIN_MINI + b.TSPIN_MINI,
			TSPIN_DOUBLE = a.TSPIN_DOUBLE + b.TSPIN_DOUBLE,
			TSPIN_TRIPLE = a.TSPIN_TRIPLE + b.TSPIN_TRIPLE,
			TSPIN_PENTA = a.TSPIN_PENTA + b.TSPIN_PENTA
		};

		return obj;
	}

	public int GetTotalClears()
		=> SINGLE + DOUBLE + TRIPLE + QUAD + PENTA +
		   TSPIN_SINGLE + TSPIN_DOUBLE + TSPIN_TRIPLE + TSPIN_QUAD +
		   TSPIN_MINI_SINGLE + TSPIN_MINI_DOUBLE +
		   PERFECT_CLEAR;


	public int NONE { get; set; }
	public int SINGLE { get; set; }
	public int DOUBLE { get; set; }
	public int TRIPLE { get; set; }
	public int QUAD { get; set; }
	public int PENTA { get; set; }
	public int TSPIN { get; set; }
	public int TSPIN_SINGLE { get; set; }
	public int TSPIN_DOUBLE { get; set; }
	public int TSPIN_TRIPLE { get; set; }
	public int TSPIN_QUAD { get; set; }
	public int TSPIN_PENTA { get; set; }
	public int TSPIN_MINI { get; set; }
	public int TSPIN_MINI_SINGLE { get; set; }
	public int TSPIN_MINI_DOUBLE { get; set; }
	public int PERFECT_CLEAR { get; set; }
}