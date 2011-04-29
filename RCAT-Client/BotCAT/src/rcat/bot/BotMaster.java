package rcat.bot;

import java.net.URI;
import java.net.URISyntaxException;


public class BotMaster {

	static int numBots = 200;
	
	//static String serverAddress = "ws://128.195.4.46:81/websocket"; // opensim
	static String serverAddress = "ws://chateau.ics.uci.edu:81/websocket";
	public static void main(String args[]) {
		URI uri;
		
		try {
			uri = new URI(serverAddress);
			for(int i = 1; i <= numBots; i++)
			(new Thread(new BotHandler(uri, 10, 500))).start();
		} catch (URISyntaxException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}

}
