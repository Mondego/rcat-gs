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

	}
}
