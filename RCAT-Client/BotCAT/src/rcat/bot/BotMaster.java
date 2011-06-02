package rcat.bot;

import java.net.URI;
import java.net.URISyntaxException;


public class BotMaster {

	//static String serverAddress = "ws://128.195.4.46:81/websocket"; // opensim

	public static void main(String args[]) {
		long time = System.currentTimeMillis();
		IO.writeToNewFile("ClientId,NumClientsConnected,ClockTime,TimeTakenToSend,NumberOfMsgReceived,Latency\n", Config.LOG_FILE, "UTF-8");
		// clientid = number assigned when creating the bot
		// clocktime = time since I started the botmanager
		// timetakentosend = time taken to send Config.LOG_FREQ msg
		// nummsgreceived = number of msg received during that timeframe
		// Latency = time taken tosend a msg and receive it back  
		for(int i = 1; i <= Config.NUM_BOTS; i++) {
			//(new Thread(new BotHandler(uri, Config.NUM_MSG, Config.FREQ))).start();
			(new Thread(new BotDude(i, time))).start();
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

	int num;
	long time;
	
	BotDude() {
	}
	BotDude(int i, long t) {
		this.num = i;
		this.time = t;
	}

	@Override
	public void run() {
		URI uri;
		try {
			uri = new URI(Config.SERVER_ADDR);
			new BotHandler2.BotManager(this.num, this.time, uri, Config.NUM_MSG - this.num*(Config.DELAY_BOTS*Config.FREQ)/1000);
			// latest bots send less msg 
		} catch (URISyntaxException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

	}
}