package rcat.bot;

import java.net.URI;
import java.net.URISyntaxException;


public class BotMaster {

	//static String serverAddress = "ws://128.195.4.46:81/websocket"; // opensim
	
	public static void main(String args[]) {
		URI uri;

		try {
			uri = new URI(Config.SERVER_ADDR);
			for(int i = 1; i <= Config.NUM_BOTS; i++) {
				try {
					Thread.sleep(Config.DELAY_BOTS);
				} catch (InterruptedException e) {
					e.printStackTrace(); 
				}
				(new Thread(new BotHandler(uri, Config.NUM_MSG, Config.FREQ))).start();
			}
		} catch (URISyntaxException e) {
			e.printStackTrace();
		}
	}

}
