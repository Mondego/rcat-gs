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
	static int MAXTOP = 400;
	static int MAXLEFT = 400;

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
		synchronized(this) {
			try {
				wait(this.freq);
			} catch (InterruptedException e) {
				// TODO Auto-generated catch block
				e.printStackTrace();
			}
		}
		// change position and send new one to server
		try {
			int posToGoTo = r.nextInt(4);
			// just checking that bot stays within visual range of screen 
			if(this.top == 0)
				posToGoTo = 1;
			if(this.top == MAXTOP)
				posToGoTo = 0;
			if(this.left == 0)
				posToGoTo = 2;
			if(this.left == MAXLEFT)
				posToGoTo = 3;	
			// actually move
			switch(posToGoTo) {
			case 0: //up
				this.top = this.top - 20;
				break;
			case 1: //down
				this.top = this.top + 20;
				break;
			case 2: //right
				this.left = this.left + 20;
				break;
			case 3: //left
				this.left = this.left - 20;
				break;
			default:
				System.out.println("Error in switch-case of move()");
			}
			send("{\"top\":" + this.top + ",\"left\":" + this.left + "}");
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
		System.out.println("Connected to: " + getURI());
		startSendingPos();
	}
	@Override
	public void onMessage(String message) {
		System.out.println(System.currentTimeMillis() + ": " + message);
	}
	@Override
	public void onClose() {
		System.out.println("Disconnected from: " + getURI());	
	}

	/**
	 * Send position to server for a given number of times
	 */
	public void startSendingPos() {
		long tid = Thread.currentThread().getId();
		System.out.println("rng seed = " + tid);
		r = new Random(tid);
		this.top = r.nextInt(20) * 20;
		this.left = r.nextInt(20) * 20;
		while(counter > 0) {
			counter --;
			waitThenMove();
		}
		try {
			close();
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}

	/**
	 * create a bot that connects to the server
	 */
	public void create() {
		long tid = Thread.currentThread().getId();
		System.out.println("Running a bot in thread#" + tid);
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


