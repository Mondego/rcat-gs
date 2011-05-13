package rcat.bot;


import java.io.IOException;
import java.net.URI;
import java.net.URISyntaxException;
import java.util.Random;

import net.tootallnate.websocket.WebSocketClient;

class Bot extends WebSocketClient {

	int counter; //number of msg to send before dying
	int freq; //frequency to send msgs at, in ms
	int top;
	int left;
	Random r;
	static int TOP_SHIFT = 4;
	static int LEFT_SHIFT = 8; // these 2 values depend on the screen to look at them. In our case, canvas in browser 
	static int MAXTOP = 140;
	static int MAXLEFT = 290;
	long millisOrigin = 0; //time origin for the experiments

	public Bot() throws URISyntaxException {
		this(new URI("ws://chateau.ics.uci.edu:81/websocket"), 1, 1000);
	}

	public Bot(int counter, int freq) throws URISyntaxException {
		this(new URI("ws://chateau.ics.uci.edu:81/websocket"), counter, freq);
	}

	/**
	 * Create new bot. 
	 * Number of msg to send: counter
	 * Frequency to send msg: freq
	 * @throws URISyntaxException 
	 */
	public Bot(URI uri, int counter, int freq) {
		super(uri);
		this.counter = counter;
		this.freq = freq;
	}

	/**
	 * wait for the given amount of time, then move up, down, left or right
	 * @param time
	 */
	public void waitThenMove() {
		try {
			botPrint("Sleeping for " + this.freq + " ms");
			Thread.sleep(this.freq);
		} catch (InterruptedException e1) {
			// TODO Auto-generated catch block
			e1.printStackTrace();
		}
		/*
		synchronized(this) {
			try {
				wait(this.freq);
			} catch (InterruptedException e) {
				// TODO Auto-generated catch block
				e.printStackTrace();
			}
		}
		*/
		// change position and send new one to server
		try {
			int posToGoTo = r.nextInt(4);
			// just checking that bot stays within visual range of screen 
			if(this.top <= 0)
				posToGoTo = 1;
			if(this.top >= MAXTOP)
				posToGoTo = 0;
			if(this.left <= 0)
				posToGoTo = 2;
			if(this.left >= MAXLEFT)
				posToGoTo = 3;	
			// actually move
			switch(posToGoTo) {
			case 0: //up
				this.top = this.top - TOP_SHIFT;
				break;
			case 1: //down
				this.top = this.top + TOP_SHIFT;
				break;
			case 2: //right
				this.left = this.left + LEFT_SHIFT;
				break;
			case 3: //left
				this.left = this.left - LEFT_SHIFT;
				break;
			default:
				botPrint("Error in switch-case of move()");
			}
			String s = "{\"t\":" + this.top + ",\"l\":" + this.left + ",\"z\":" + 666 + "}"; // 666 is just a dummy number, we dont use z for now
			send(s); 
			botPrint("Sent " + s);
			//} catch (ClosedChannelException e) {
			//	connect();
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

	}

	/**
	 * when connection opens, start sending my position 
	 */
	@Override
	public void onOpen() {
		botPrint("Socket opened to: " + getURI());
		startSendingPos();
	}
	@Override
	public void onMessage(String message) {
		botPrint("Received: " + message);
	}
	@Override
	public void onClose() {
		botPrint("Socket closed from: " + getURI());	
	}

	/**
	 * Send position to server for a given number of times
	 */
	public void startSendingPos() {
		long tid = Thread.currentThread().getId();
		botPrint("RNG seed = " + tid);
		r = new Random(tid);
		this.top = r.nextInt(MAXTOP);
		this.left = r.nextInt(MAXLEFT);
		while(counter > 0) {
			counter --;
			waitThenMove();
		}
		try {
			Thread.sleep(1000);
		} catch (InterruptedException e1) {
			// TODO Auto-generated catch block
			e1.printStackTrace();
		}
		
		try {
			close();
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}

	public void botPrint(String msg) {
		long tid = Thread.currentThread().getId();
		System.out.println("t-" + tid + ":" + (System.currentTimeMillis() - this.millisOrigin) + " \t " + msg);
	}
	/**
	 * create a bot that connects to the server
	 */
	public void create() {
		this.millisOrigin = System.currentTimeMillis();
		botPrint("Bot created");
		connect();
	} 


}




public class BotHandler implements Runnable {

	public Bot bot;

	public BotHandler(URI adress, int counter, int freq) {
		bot = new Bot(adress, counter, freq);	
	}

	@Override
	public void run() {
		bot.create();
	}


}


