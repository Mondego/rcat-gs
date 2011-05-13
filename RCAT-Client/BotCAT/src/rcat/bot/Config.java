package rcat.bot;

import java.io.IOException;
import java.util.Properties;

public class Config {
	public static String SERVER_ADDR;
	public static int NUM_BOTS;
	public static int NUM_MSG;
	public static int FREQ;

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
		FREQ = Integer.parseInt(prop.getProperty("freq"));

	}
}
