package rcat.bot;

import java.net.URI;
import java.net.URISyntaxException;


public class BotMaster {

	//static String serverAddress = "ws://128.195.4.46:81/websocket"; // opensim
	
	public static void main(String args[]) {
			for(int i = 1; i <= Config.NUM_BOTS; i++) {
				//(new Thread(new BotHandler(uri, Config.NUM_MSG, Config.FREQ))).start();
				(new Thread(new BotDude())).start();
				/*
				 java.awt.EventQueue.invokeLater(new Runnable() {
				 
					@Override
					public void run() {
						new BotHandler2.BotManager(uri, Config.NUM_MSG, Config.FREQ);
					}
				});
				*/
				try {
					Thread.sleep(Config.DELAY_BOTS);
				} catch (InterruptedException e) {
					e.printStackTrace(); 
				}	
			}
	}

}

class BotDude implements Runnable{
	
	BotDude() {
	}

	@Override
	public void run() {
		URI uri;
		try {
			uri = new URI(Config.SERVER_ADDR);
			new BotHandler2.BotManager(uri, Config.NUM_MSG, Config.FREQ);
		} catch (URISyntaxException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
		
	}
}