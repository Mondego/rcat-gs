package rcat.bot;

import java.io.*;
import java.nio.ByteBuffer;
import java.nio.channels.FileChannel;
import java.nio.charset.*;
import java.util.Collection;

/**
 * Copyright (C) 2009
 * 
 * @author Yasser Ganjisaffar <yganjisa at uci dot edu>
 */

public final class IO {

	private static final int BSIZE = 8192;

	public static String getFileContent(String filename, String charset) {
		StringBuilder contentBuffer = new StringBuilder((int) (new File(
				filename)).length());

		if (charset.equals("")) {
			charset = System.getProperty("file.encoding");
		}
		try {
			FileChannel fc = new FileInputStream(filename).getChannel();
			ByteBuffer buff = ByteBuffer.allocate(BSIZE);

			while (fc.read(buff) != -1) {
				buff.flip();
				String result = "";
				result += Charset.forName(charset).decode(buff);
				contentBuffer.append(result);
				buff.clear();
			}
			fc.close();
		} catch (IOException e) {
			System.out.println(e.getMessage());
			return "";
		}
		return contentBuffer.toString();
	}

	public static boolean getLineSeparatedItemsFromFile(String filename,
			Collection<String> col) {
		try {
			return getLineSeparatedItemsFromFile(new FileInputStream(filename),
					col);
		} catch (FileNotFoundException e) {
			e.printStackTrace();
		}
		return false;
	}
	
	public static boolean getLineSeparatedItemsFromFile(InputStream is,
			Collection<String> col) {
		try {
			BufferedReader input = new BufferedReader(new InputStreamReader(is));
			String line = null;
			while ((line = input.readLine()) != null) {
				col.add(line.trim());
			}
		} catch (Exception e) {			
			return false;
		}
		return true;
	}
	
	public static String getFileContent(String filename) {
		StringBuilder contentBuffer = new StringBuilder();
		BufferedReader input;
		try {
			input = new BufferedReader(new FileReader(filename));
			String line = null;
			while ((line = input.readLine()) != null) {
				contentBuffer.append(line.trim() + "\n");
			}
			input.close();
		} catch (Exception e) {
			e.printStackTrace();
		}
		return contentBuffer.toString();
	}

	public static String getResourceContent(String filename) {
		StringBuilder contentBuffer = new StringBuilder();
		BufferedReader input;
		try {
			InputStream is = IO.class.getClassLoader().getResourceAsStream(
					filename);
			if (is == null) {
				return null;
			}
			input = new BufferedReader(new InputStreamReader(is));
			String line = null;
			while ((line = input.readLine()) != null) {
				contentBuffer.append(line.trim() + "\n");
			}
			input.close();
		} catch (Exception e) {
			e.printStackTrace();
		}
		return contentBuffer.toString();
	}
	
	public static void populateItemsFromResource(String filename, Collection<String> col) {		
		BufferedReader input;
		try {
			InputStream is = IO.class.getClassLoader().getResourceAsStream(
					filename);
			if (is == null) {
				return;
			}
			input = new BufferedReader(new InputStreamReader(is));
			String line = null;
			while ((line = input.readLine()) != null) {
				col.add(line.trim());
			}
			input.close();
		} catch (Exception e) {
			e.printStackTrace();
		}		
	}

	public static void replaceSubStrInFile(String oldStr, String newStr,
			String filename) {
		String content = getFileContent(filename, "");
		content = content.replaceAll(oldStr, newStr);
		try {
			DataOutputStream out = new DataOutputStream(
					new BufferedOutputStream(new FileOutputStream(filename)));
			out.writeBytes(content);
			out.close();
		} catch (IOException e) {
			e.printStackTrace();
		}
	}

	public static String getCurrentPath() {
		File curDir = new File(".");
		String Path = "";
		try {
			Path = curDir.getCanonicalPath();
			Path = Path.replaceAll("\\\\", "/");
		} catch (Exception e) {
			e.printStackTrace();
		}
		return Path;
	}

	public static FilenameFilter filter(final String filesuffix) {
		return new FilenameFilter() {

			public boolean accept(File dir, String name) {
				return name.endsWith(filesuffix);
			}
		};
	}

	public static void fileCopy(String source, String destination)
			throws Exception {
		FileChannel in = new FileInputStream(source).getChannel(), out = new FileOutputStream(
				destination).getChannel();
		ByteBuffer buffer = ByteBuffer.allocate(BSIZE);
		while (in.read(buffer) != -1) {
			buffer.flip(); // Prepare for writing
			out.write(buffer);
			buffer.clear(); // Prepare for reading
		}
	}

	public static void writeToNewFile(String fileContent, String destination, String charset) {
		File f = new File(destination);
		if(f.exists())
			f.delete(); //we want a new empty file
		writeToFile(fileContent, destination, charset);
	}
	
	//write fileContent in destination using specified charset 
	public static void writeToFile(String fileContent, String destination,
			String charset) {
		try {
			FileChannel fc = new FileOutputStream(destination).getChannel();
			if (charset.equals("")) {
				fc.write(ByteBuffer.wrap((fileContent).getBytes()));
			} else {
				fc.write(ByteBuffer.wrap((fileContent).getBytes(charset)));
			}
			fc.close();
		} catch (Exception e) {
			e.printStackTrace();
		}
	}
	
	
	public static void writeToExistingFile(String fileContent, String destination,
			String charset) {
		try {

			File file = new File(destination);
			    boolean exists = file.exists();
			
			FileChannel fc = new FileOutputStream(destination,exists).getChannel();
			if (charset.equals("")) {
				fc.write(ByteBuffer.wrap((fileContent).getBytes()));
			} else {
				fc.write(ByteBuffer.wrap((fileContent).getBytes(charset)));
			}

			fc.close();
		} catch (Exception e) {
			e.printStackTrace();
		}
	}


	public static String getFileType(String filename) {
		if (filename.indexOf(".") < 0) {
			return "";
		}
		return filename.substring(filename.lastIndexOf(".") + 1);
	}
	
	public static boolean deleteFolder(File folder) {
		return deleteFolderContents(folder) && folder.delete();
	}
	
	public static boolean deleteFolderContents(File folder) {
		File[] files = folder.listFiles();
		for (File file : files) {
			if (file.isFile()) {
				if (!file.delete()) {
					return false;
				}
			} else {
				if (!deleteFolder(file)) {
					return false;
				}
			}
		}
		return true;
	}
}
