package rcat.bot;

import java.io.IOException;
import java.net.URI;
import java.util.ArrayList;
import java.util.Random;
import java.util.Timer;
import java.util.TimerTask;

import json.JSONArray;
import json.JSONException;
import json.JSONObject;
import net.tootallnate.websocket.WebSocketClient;
import net.tootallnate.websocket.WebSocketDraft;


public class LogBot extends WebSocketClient {
	long millisOrigin; //initiated in BotManager
	long lastLogTime; //millis since last time I flushed to disk
	BotManager bm;
	int botId;
	StringBuilder sb;
	int latency = -1;
	int numPosMsgReceived;
	int topToWatch;
	int leftToWatch;
	long latencyStart;
	ArrayList<String> onlineUsers;

	public LogBot(BotManager bm, int num, long time, URI uri, WebSocketDraft draft) {
		super(uri, draft);
		this.millisOrigin = time; //all bots share the same t=0
		this.botId = num;
		this.bm = bm;
		sb = new StringBuilder();
		//System.out.println("Bot created.");
	}

	/**
	 * Bot's position just changed. Track 
	 */
	public void changeTrackedPositionAndLog(int top, int left) {
		long now = System.currentTimeMillis();
		// re-init the variables to be logged
		if(this.latency != -1) { // the message to look for arrived 
			sb.append(this.botId + "," 
					+ this.onlineUsers.size() + "," 
					+ (now - this.millisOrigin) + "," 
					+ (now - this.lastLogTime) + "," 
					+ this.numPosMsgReceived + ","
					+ this.latency + "\n");
			this.lastLogTime = now;
			this.latency = -1;
			this.numPosMsgReceived = 0;
		}
		// whether or not I received the msg to look at, change the msg to look at 
		this.topToWatch = top;
		this.leftToWatch = left;
		this.latencyStart = now;

	}

	// flush the string buffer to disk
	public synchronized void flushLog() {
		IO.writeToExistingFile(sb.toString(), Config.LOG_FILE, "UTF-8");
		// make a new sb
		sb.delete(0, sb.length());
	}

	@Override
	public void send(String msg) {
		try {
			super.send(msg);
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}
	@Override
	public void onMessage(String strmsg) {
		//System.out.println(strmsg);
		// pos updates	
		try {
			JSONObject jsmsg = new JSONObject(strmsg);
			JSONObject data = jsmsg.getJSONObject("Data");
			switch(jsmsg.getInt("Type")) {
			case 1: //disconnection
				String discoUserName = data.getString("n");
				this.onlineUsers.remove(discoUserName);
				//System.out.println("Bot "+ this.botId +" : removed user " + discoUserName + " from local game state");
				break;

			case 2: //pos update
				this.numPosMsgReceived ++;
				JSONObject pos = data.getJSONObject("p");
				int t = pos.getInt("t");
				int l = pos.getInt("l");
				if(t == this.topToWatch && l == this.leftToWatch) { // msg tracked to determine client-to-client latency
					this.latency = (int) (System.currentTimeMillis() - this.latencyStart);
				}
				// add user to my local collection of users
				String coUserName2 = data.getString("n");
				if(! (this.onlineUsers.contains(coUserName2))) {
					this.onlineUsers.add(coUserName2);
					//System.out.println("Bot "+ this.botId +" : added user " + coUserName2 + " to local game state");
				}

				break;
			case 3: //on connect: list of all connected users
				JSONArray users = data.getJSONArray("Users");
				JSONObject user;
				String coUserName3;
				for(int i = 0; i < users.length(); i++) {
					user = users.getJSONObject(i);
					coUserName3 = user.getString("n");
					if(! (this.onlineUsers.contains(coUserName3))) {
						this.onlineUsers.add(coUserName3);
						//System.out.println("Bot "+ this.botId +" : added user " + coUserName3 + " to local game state");
					}
				}
				break;
			default:
				System.out.println("Error: Type in JSON msg was neither 1, 2, or 3");
			}
		} catch (JSONException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}		
	}

	@Override
	public void onOpen() {
		this.lastLogTime = System.currentTimeMillis();
		this.onlineUsers = new ArrayList<String>();
		bm.notifyConnected();
		System.out.println("Logging Bot "+ botId +" connected.");
		this.latencyStart = System.currentTimeMillis();
	}

	@Override
	public void onClose() {
		System.out.println("Logging Bot "+ botId +" disconnected.");
		onlineUsers.clear();
	}

	/**
	 * manages what the websocket client connections and msg sent
	 * @author tho
	 *
	 */
	public static class BotManager {
		public static final long serialVersionUID = -6056260699202978657L;

		public LogBot cc;
		int top;
		int left;
		Random r;
		int posToGoTo = 1; //bots always go down
		TimerTask doUpdatePosAndSend;
		TimerTask doClose;
		Timer timer;
		Timer ctimer;
		int numMsgSent;
		int botid; 

		public BotManager(final int num, long time, final URI uri, final int numMsg) {

			//this.r = new Random(Thread.currentThread().getId() + Config.MACHINE_SEED);
			//this.top = r.nextInt(Config.MAX_TOP);
			//this.left = r.nextInt(Config.MAX_LEFT);

			this.top = 0; //0 is just a hardcoded value so that I can see bots popping on the screen and disappear later on
			this.left = Config.MACHINE_SEED + num;

			// connect
			cc = new LogBot(this, num, time, uri, WebSocketDraft.DRAFT76);
			cc.connect();
			this.botid = num;

			// send	task
			doUpdatePosAndSend = new TimerTask() {
				@Override
				public void run() {
					updatePos();
					cc.send(getStrFromPos());
					numMsgSent ++;
					// log every LOG_FREQ msg
					if(numMsgSent % Config.LOG_FREQ == (Config.LOG_FREQ-1)) {
						cc.changeTrackedPositionAndLog(getTop(), getLeft());
					}			
					// flush every FLUSH_FREQ msg
					if(numMsgSent % Config.FLUSH_FREQ == (Config.FLUSH_FREQ-1)) { 
						cc.flushLog();
					}
					if(numMsgSent >= numMsg) { // I'm done sending all msg 
						timer.cancel();
						ctimer = new Timer();
						ctimer.schedule(doClose, num*Config.SLEEP_CLOSE);
					}
				}
			};
			// close task
			doClose = new TimerTask() {
				@Override
				public void run() {
					try {
						cc.close();
						ctimer.cancel();
					} catch (IOException e) {
						// TODO Auto-generated catch block
						e.printStackTrace();
					}
				}
			};
		} 		//end of botmanager constructor

		/**
		 * called by bothandler when connection has been made
		 * then manager can start sending msg
		 */
		public void notifyConnected() {
			this.timer = new Timer();
			this.numMsgSent = -2; //yeah thats weird, but otherwise the log says it took us 900ms to send 1000ms of msg ...
			this.timer.scheduleAtFixedRate(doUpdatePosAndSend, Config.SLEEP_START, (int) (1000/Config.FREQ));
		}

		/**
		 * return a string of the current bot position 
		 */
		private String getStrFromPos() {
			// 666 is just a dummy number, we dont use z for now
			return("{\"t\":" + this.top + ",\"l\":" + this.left + ",\"z\":" + 666 + "}"); 
		}

		/**
		 * update bot's current position (either up or down)
		 */
		private void updatePos() {
			/*
			if(this.top <= 0)
				posToGoTo = 1;
			if(this.top >= Config.MAX_TOP)
				posToGoTo = 0;
			// actually move
			switch(posToGoTo) {
			case 0: //up
				this.top = this.top - Config.TOP_SHIFT;
				break;
			case 1: //down
				this.top = this.top + Config.TOP_SHIFT;
				break;
			default:
				System.out.println("Error in switch case of posupdate");
			}
			 */
			this.top = this.top + Config.TOP_SHIFT; //bot keeps going down
		}


		/**
		 * getters for top and left
		 */
		private int getTop() {
			return this.top;
		}
		private int getLeft() {
			return this.left;
		}

		/**
		 * print in stdout the thread/bot id and time since bot creation
		 * @param msg the message to print
		 */


	}
	//end of public class BotHandler2 


}
