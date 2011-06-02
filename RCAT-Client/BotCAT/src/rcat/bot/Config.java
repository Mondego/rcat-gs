package rcat.bot;

import java.io.IOException;
import java.util.Properties;

public class Config {
	public static String SERVER_ADDR;
	public static int NUM_BOTS;
	public static int NUM_MSG;
	public static int FREQ;
	public static int SLEEP_CLOSE;
	public static int DELAY_BOTS; 
	public static int TOP_SHIFT;
	public static int LEFT_SHIFT; // these 2 values depend on the screen to look at them. In our case, canvas in browser 
	public static int MAX_TOP;
	public static int MAX_LEFT;
	public static int SLEEP_START;
	public static String LOG_FILE;
	public static int FLUSH_FREQ;
	public static int LOG_FREQ;
	public static int MACHINE_SEED;
	static {
		Properties prop = new Properties();
		try {
			prop.load(Config.class.getClassLoader().getResourceAsStream(
			"bot.properties"));
		} catch (IOException e) {
			e.printStackTrace();
		}
		SERVER_ADDR = prop.getProperty("server.addr");
		NUM_BOTS = Integer.parseInt(prop.getProperty("num.bots"));
		NUM_MSG = Integer.parseInt(prop.getProperty("num.msg"));
		DELAY_BOTS = Integer.parseInt(prop.getProperty("delay.between.bots"));
		FREQ = Integer.parseInt(prop.getProperty("freq"));
		SLEEP_CLOSE = Integer.parseInt(prop.getProperty("bot.sleep.before.close"));
		TOP_SHIFT = Integer.parseInt(prop.getProperty("top.shift"));
		LEFT_SHIFT = Integer.parseInt(prop.getProperty("left.shift"));
		MAX_TOP = Integer.parseInt(prop.getProperty("max.top"));
		MAX_LEFT = Integer.parseInt(prop.getProperty("max.left"));
		SLEEP_START = Integer.parseInt(prop.getProperty("bot.sleep.before.start"));
		LOG_FILE = prop.getProperty("log.file");
		FLUSH_FREQ = Integer.parseInt(prop.getProperty("flush.freq"));
		LOG_FREQ = Integer.parseInt(prop.getProperty("log.freq"));
		MACHINE_SEED = Integer.parseInt(prop.getProperty("machine.seed"));
		

	}
}
