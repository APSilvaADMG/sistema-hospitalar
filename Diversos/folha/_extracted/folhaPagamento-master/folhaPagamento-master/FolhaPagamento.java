package folhaPagamento;

import java.util.Scanner;

public class FolhaPagamento {

	public static void main(String[] args) {
		int selecaoMPrincipal = 0;
		
		Scanner input = new Scanner(System.in);
		
		showMenuPrincipal();
		
		selecaoMPrincipal = input.nextInt();
		
		calculoFolha(selecaoMPrincipal);
		
		input.close();
	}
	
	public static void showMenuPrincipal() {
		
		System.out.println("Ol√°!\n\nSeja Bem Vindo(a) ao Sistema\n..::FOLHA DE PAGAMENTO::..\n");
		System.out.println("Escolha o n√∫mero referente ao Grupo de Funcion√°rios que deseja calcular a Folha de Pagamento:");
		System.out.printf("%-5s%20s%n%-5s%20s%n%-5s%20s%n%-5s%20s%n%s%n%-5s%4s%n", "1.","Funcion√°rios Assalariados",
									  								  "2.", "Funcion√°rios Por Hora",
									  								  "3.", "Funcion√°rios Comissionados",
									  								  "4.", "Funcion√°rios Assalariados - Comissionados",
									  								  "-------------------",
									  								  "0.", "Sair");
	}
	
	public static void calculoFolha(int selecaoMPrincipal) {
		
		Scanner input = new Scanner(System.in);
		
		switch (selecaoMPrincipal) {
			case 0:
				System.out.println("\nSistema Finalizado...\n");
				break;
			case 1:
				System.out.println("\nFuncion√°rios Assalariados:\n");
				Assalariado asl = new Assalariado("Julio", "Software", "11-04-2016", 682);
				
<<<<<<< HEAD
				System.out.printf("%-15s%-15s%-20s%-15s%n", "Nome:", "Setor:", "Dt. Admiss„o:", "Sal·rio Fixo:");
				System.out.printf("%-15s%-15s%-20sR$ %-15.2f%n%n", asl.getNome(), asl.getSetor(), asl.getDataAdm(), asl.getSalFixo());
=======
				System.out.printf("%-15s%-15s%-20s%-15s%n", "Nome:", "Setor:", "Dt. Admiss√£o:", "Sal√°rio Fixo:");
				System.out.printf("%-15s%-15s%-20sR$ %-15s%n%n", asl.getNome(), asl.getSetor(), asl.getDataAdm(), asl.getSalFixo());
>>>>>>> origin/master
				
				showMenuPrincipal();
				selecaoMPrincipal = input.nextInt();
				calculoFolha(selecaoMPrincipal);
				break;
			case 2:
				System.out.println("\nFuncion√°rios Por Hora:\n");
				Horista hr = new Horista("Andr√©", "Laser", "19-12-2016", 682, 15);
				
<<<<<<< HEAD
				System.out.printf("%-15s%-15s%-20s%-20s%-20s%n", "Nome:", "Setor:", "Dt. Admiss„o:", "Sal·rio Fixo:", "Sal·rio + H.E.:");
=======
				System.out.printf("%-15s%-15s%-20s%-20s%-20s%n", "Nome:", "Setor:", "Dt. Admiss√£o:", "Sal√°rio Fixo:", "Sal√°rio + H.E.:");
>>>>>>> origin/master
				System.out.printf("%-15s%-15s%-20sR$ %-17.2fR$ %,.2f%n%n", hr.getNome(), hr.getSetor(), hr.getDataAdm(), hr.getSalFixo(), hr.getCalculoHorista(hr.getSalFixo(), hr.getHe()));
				
				showMenuPrincipal();
				selecaoMPrincipal = input.nextInt();
				calculoFolha(selecaoMPrincipal);
				break;
			case 3:
<<<<<<< HEAD
				System.out.println("\nFuncion·rios Comissionados:\n");
				Comissionado cms = new Comissionado("Pedro", "Suprimentos", "15-06-2011", 6000, 0.15);
				
				System.out.printf("%-15s%-15s%-20s%-20s%-20s%n", "Nome:", "Setor:", "Dt. Admiss„o:", "Total de Vendas:", "Sal·rio(" + (int)(cms.getPercentual() * 100) + "% sobre Vendas)");
				System.out.printf("%-15s%-15s%-20sR$ %-17.2fR$ %,.2f%n%n", cms.getNome(), cms.getSetor(), cms.getDataAdm(), cms.getTotVendas(), cms.getCalculoComissionado(cms.getTotVendas(), cms.getPercentual()));
=======
				System.out.println("\nFuncion√°rios Comissionados:\n");
				Comissionado cms = new Comissionado("Pedro", "Suprimentos", "15-06-2011", 6000, 0.08);
				
				System.out.printf("%-15s%-15s%-20s%-20s%-20s%n", "Nome:", "Setor:", "Dt. Admiss√£o:", "Total de Vendas:", "Sal√°rio(" + cms.getPercentual() + "% sobre Vendas)");
				System.out.printf("%-15s%-15s%-20sR$ %-17.2fR$ %,.2f%n%n", cms.getNome(), cms.getSetor(), cms.getDataAdm(), cms.getTotVendas(), cms.getTotComissionado(cms.getTotVendas(), cms.getPercentual()));
>>>>>>> origin/master
				
				showMenuPrincipal();
				selecaoMPrincipal = input.nextInt();
				calculoFolha(selecaoMPrincipal);
				break;
			case 4:
<<<<<<< HEAD
				
				int escolha = 0;
				
				System.out.println("\nFuncion·rios Assalariados - Comissionados:\n");
				AssalariadoComissionado aslcms = new AssalariadoComissionado("Talita", "Financeiro", "01-07-2015", 852, 0.08, 7000);
				
				System.out.printf("%-15s%-15s%-20s%-20s%-20s%-20s%n", "Nome:", "Setor:", "Dt. Admiss„o:", "Sal·rio Base:", "Total de Vendas:", "Sal·rio(" + (int)(aslcms.getPercentual() * 100) + "% sobre Vendas):");
				System.out.printf("%-15s%-15s%-20sR$ %-17.2fR$ %-17.2fR$ %-17.2f%n%n", 
				aslcms.getNome(), aslcms.getSetor(), aslcms.getDataAdm(), aslcms.getSalBase(), aslcms.getTotVendas(), aslcms.getCalculoAssalComiss(aslcms.getSalBase(), aslcms.getTotVendas(), aslcms.getPercentual()));
				
				System.out.println("ESTE M S EST¡ PROGRAMADO AUMENTO DE 10% NO SAL¡RIO BASE PARA ESTE GRUPO DE FUNCION¡RIOS.\n\nDeseja Calcular a Folha novamente com o aumento de 10%?\n>>Escolha o N˙mero Correspondente ‡ Resposta");
				System.out.println("(1) SIM\n(2) N√O");
				escolha = input.nextInt();
				
				if (escolha == 1) {
					System.out.printf("%-15s%-15s%-20s%-20s%-20s%-20s%n", "Nome:", "Setor:", "Dt. Admiss„o:", "NOVO Sal·rio Base:", "Total de Vendas:", "Sal·rio(" + (int)(aslcms.getPercentual() * 100) + "% sobre Vendas):");
					System.out.printf("%-15s%-15s%-20sR$ %-17.2fR$ %-17.2fR$ %-17.2f%n%n", 
					aslcms.getNome(), aslcms.getSetor(), aslcms.getDataAdm(), aslcms.getSalBaseNovo(0.10), aslcms.getTotVendas(), aslcms.getCalculoNovoAssalComiss(aslcms.getSalBaseNovo(0.10), aslcms.getTotVendas(), aslcms.getPercentual(), aslcms.getPercAumento(0.10)));
					
					showMenuPrincipal();
					selecaoMPrincipal = input.nextInt();
					calculoFolha(selecaoMPrincipal);						
				}else {
					if (escolha == 2) {
						showMenuPrincipal();
						selecaoMPrincipal = input.nextInt();
						calculoFolha(selecaoMPrincipal);						
					}else {
						System.out.println("Digite um n˙mero v·lido!");
						System.out.println("Sistema Finalizado...");
						break;
						
					}
				}
				
				
				
				
				
=======
				System.out.println("\nFuncion√°rios Assalariados - Comissionados:\n");
>>>>>>> origin/master
				break;
			default:
				System.out.println("\nDigite um n√∫mero v√°lido.\n");
				
				showMenuPrincipal();
				selecaoMPrincipal = input.nextInt();
				calculoFolha(selecaoMPrincipal);
				break;
				
		}
		input.close();
	}
	
	
}
