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
		for(int i = 1; i<= Config.NUM_LOGBOTS; i++) {
			(new Thread(new BotDude(i, time, true))).start();
			try {
				Thread.sleep(Config.DELAY_BOTS);
			} catch (InterruptedException e) {
				e.printStackTrace(); 
			}
		}
		for(int i = 1 ; i <= Config.NUM_DUMBOTS; i++) {
			//(new Thread(new BotHandler(uri, Config.NUM_MSG, Config.FREQ))).start();
			(new Thread(new BotDude(i, time, false))).start();
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
	boolean isLogging;
	
	BotDude() {
	}
	BotDude(int i, long t, boolean b) {
		this.num = i;
		this.time = t;
		this.isLogging = b;
	}

	@Override
	public void run() {
		URI uri;
		try {
			uri = new URI(Config.SERVER_ADDR);
			if(this.isLogging)
				new LogBot.BotManager(this.num, this.time, uri, Config.NUM_MSG - this.num*(Config.DELAY_BOTS*Config.FREQ)/1000);
			else
				new DumBot.BotManager(this.num, this.time, uri, Config.NUM_MSG - this.num*(Config.DELAY_BOTS*Config.FREQ)/1000);
			// latest bots send less msg 
		} catch (URISyntaxException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

	}
}