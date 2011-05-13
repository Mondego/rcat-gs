package rcat.bot;

import java.net.URI;
import java.net.URISyntaxException;


public class BotMaster {

	//static String serverAddress = "ws://128.195.4.46:81/websocket"; // opensim
	static String serverAddress = "ws://chateau.ics.uci.edu:81/websocket";


	public static void main(String args[]) {
		URI uri;

		try {
			uri = new URI(Config.SERVER_ADDR);
			for(int i = 1; i <= Config.NUM_BOTS; i++) {
				try {
					Thread.sleep(1000);
				} catch (InterruptedException e) {
					// TODO Auto-generated catch block
					e.printStackTrace();
				}
				(new Thread(new BotHandler(uri, Config.NUM_MSG, Config.FREQ))).start();
			}
		} catch (URISyntaxException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}

}
